#nullable enable

using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.FrameSync.Rollback;
using AbilityKit.Ability.Host.Extensions.Client.FrameSync;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.View
{
    public sealed class ShooterClientFrameSyncController
    {
        private readonly IShooterBattleRuntimePort _runtime;
        private readonly ShooterPresentationFacade _presentation;
        private readonly ShooterPackedSnapshotSyncController _snapshotSync;
        private readonly ClientPredictionReconciliationCoordinator<ShooterPlayerCommand> _predictionReconciliation = new ClientPredictionReconciliationCoordinator<ShooterPlayerCommand>();
        private readonly RollbackCoordinator _rollback;
        private readonly ShooterClientDriftRecoveryPolicy _recoveryPolicy;
        private readonly float _fixedDeltaTime;
        private float _accumulator;
        private int _catchUpTargetFrame;

        public ShooterClientFrameSyncController(IShooterBattleRuntimePort runtime, ShooterPresentationFacade presentation, int tickRate)
            : this(runtime, presentation, tickRate, null)
        {
        }

        public ShooterClientFrameSyncController(IShooterBattleRuntimePort runtime, ShooterPresentationFacade presentation, int tickRate, ShooterGatewaySnapshotDecoder? decoder)
            : this(runtime, presentation, tickRate, decoder, 0ul, 240)
        {
        }

        public ShooterClientFrameSyncController(IShooterBattleRuntimePort runtime, ShooterPresentationFacade presentation, int tickRate, ShooterGatewaySnapshotDecoder? decoder, ulong rollbackWorldId, int rollbackBufferFrames)
            : this(runtime, presentation, tickRate, decoder, rollbackWorldId, rollbackBufferFrames, ShooterClientDriftRecoveryPolicy.Default)
        {
        }

        public ShooterClientFrameSyncController(
            IShooterBattleRuntimePort runtime,
            ShooterPresentationFacade presentation,
            int tickRate,
            ShooterGatewaySnapshotDecoder? decoder,
            ulong rollbackWorldId,
            int rollbackBufferFrames,
            ShooterClientDriftRecoveryPolicy recoveryPolicy)
        {
            if (tickRate <= 0) throw new ArgumentOutOfRangeException(nameof(tickRate));
            if (rollbackBufferFrames <= 0) throw new ArgumentOutOfRangeException(nameof(rollbackBufferFrames));

            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            _snapshotSync = decoder == null
                ? new ShooterPackedSnapshotSyncController(_runtime, _presentation)
                : new ShooterPackedSnapshotSyncController(_runtime, _presentation, decoder);
            var registry = new RollbackRegistry();
            registry.Register(new ShooterPackedSnapshotRollbackProvider(_runtime, rollbackWorldId));
            _rollback = new RollbackCoordinator(registry, new RollbackSnapshotRingBuffer(rollbackBufferFrames));
            _recoveryPolicy = recoveryPolicy;
            _fixedDeltaTime = 1f / tickRate;
        }

        public int CurrentFrame => _runtime.CurrentFrame;

        public float FixedDeltaTime => _fixedDeltaTime;

        public float AccumulatedTime => _accumulator;

        public int PendingInputFrameCount => _predictionReconciliation.PendingInputFrameCount;

        public ShooterSnapshotApplyResult LastSnapshotApplyResult { get; private set; } = ShooterSnapshotApplyResult.Ignored;

        public ShooterClientReconciliationResult LastReconciliationResult { get; private set; } = ShooterClientReconciliationResult.None;

        public bool NeedsFullSnapshotResync { get; private set; }

        public ShooterClientRecoveryState RecoveryState { get; private set; } = ShooterClientRecoveryState.Normal;

        public ShooterClientResyncReason LastResyncReason { get; private set; } = ShooterClientResyncReason.None;

        public int LastResyncClientFrame { get; private set; }

        public int LastResyncAuthoritativeFrame { get; private set; }

        public uint LastResyncClientStateHash { get; private set; }

        public uint LastResyncAuthoritativeStateHash { get; private set; }

        public bool TryRestorePredictedSnapshot(int frame)
        {
            if (!_rollback.TryRestore(new FrameIndex(frame)))
            {
                return false;
            }

            _accumulator = 0f;
            PublishRuntimeSnapshot();
            return true;
        }

        public ShooterClientFrameTickResult CatchUpToFrame(int targetFrame)
        {
            if (!_runtime.IsStarted)
            {
                return ShooterClientFrameTickResult.NotStarted;
            }

            if (RecoveryState == ShooterClientRecoveryState.AwaitingFullSnapshot || RecoveryState == ShooterClientRecoveryState.ApplyingFullSnapshot)
            {
                return new ShooterClientFrameTickResult(0, _runtime.CurrentFrame, _runtime.ComputeStateHash());
            }

            if (targetFrame <= _runtime.CurrentFrame)
            {
                _accumulator = 0f;
                return new ShooterClientFrameTickResult(0, _runtime.CurrentFrame, _runtime.ComputeStateHash());
            }

            var ticks = 0;
            while (_runtime.CurrentFrame < targetFrame)
            {
                if (!StepRuntimeFrameAndCapture())
                {
                    break;
                }

                ticks++;
            }

            _accumulator = 0f;
            if (ticks > 0)
            {
                PublishRuntimeSnapshot();
            }

            return new ShooterClientFrameTickResult(ticks, _runtime.CurrentFrame, _runtime.ComputeStateHash());
        }

        public int SubmitLocalInput(in ShooterPlayerCommand command)
        {
            return SubmitLocalInputs(new[] { command });
        }

        public int SubmitLocalInputs(ShooterPlayerCommand[] commands)
        {
            if (commands == null || commands.Length == 0)
            {
                return 0;
            }

            if (RecoveryState == ShooterClientRecoveryState.AwaitingFullSnapshot || RecoveryState == ShooterClientRecoveryState.ApplyingFullSnapshot)
            {
                return 0;
            }

            var frame = _runtime.CurrentFrame;
            var accepted = _runtime.SubmitInput(frame, commands);
            if (accepted > 0)
            {
                RecordPendingInput(frame, commands);
            }

            return accepted;
        }

        public ShooterSnapshotApplyResult ApplyGatewayPush(uint opCode, ArraySegment<byte> payload)
        {
            var replayTargetFrame = _runtime.CurrentFrame;
            var predictedHashBeforeCorrection = _runtime.IsStarted ? _runtime.ComputeStateHash() : 0u;
            var wasAwaitingFullSnapshot = NeedsFullSnapshotResync;

            LastSnapshotApplyResult = _snapshotSync.TryApplyGatewayPush(opCode, payload);
            if (LastSnapshotApplyResult == ShooterSnapshotApplyResult.AppliedPackedSnapshot)
            {
                var authoritativeFrame = _snapshotSync.LastAppliedFrame;
                var authoritativeStateHash = _snapshotSync.LastAppliedStateHash;
                var importedStateHash = _runtime.ComputeStateHash();
                var isStrongRecoverySnapshot = IsStrongRecoverySnapshot(_snapshotSync.LastAppliedSnapshotFlags);

                RecoveryState = wasAwaitingFullSnapshot ? ShooterClientRecoveryState.ApplyingFullSnapshot : ShooterClientRecoveryState.Normal;
                _accumulator = 0f;
                CaptureRollbackSnapshot();
                var reconciliation = new ShooterClientReconciliationResult(
                    LastSnapshotApplyResult,
                    _predictionReconciliation.ReconcileAfterAuthoritativeSnapshot(
                        replayTargetFrame,
                        predictedHashBeforeCorrection,
                        authoritativeFrame,
                        authoritativeStateHash,
                        importedStateHash,
                        _runtime.CurrentFrame,
                        () => _runtime.CurrentFrame,
                        () => _runtime.ComputeStateHash(),
                        (frame, commands) => _runtime.SubmitInput(frame, commands),
                        StepRuntimeFrameAndCapture));

                LastReconciliationResult = reconciliation;
                if (reconciliation.AuthoritativeHashMatched)
                {
                    var frameDelta = replayTargetFrame - authoritativeFrame;
                    if (wasAwaitingFullSnapshot && !isStrongRecoverySnapshot)
                    {
                        MarkFullSnapshotResyncNeeded(
                            LastResyncReason == ShooterClientResyncReason.None ? ShooterClientResyncReason.AuthoritativeHashMismatch : LastResyncReason,
                            replayTargetFrame,
                            authoritativeFrame,
                            predictedHashBeforeCorrection,
                            authoritativeStateHash);
                    }
                    else if (Math.Abs(frameDelta) > _recoveryPolicy.ReplayThreshold)
                    {
                        MarkFullSnapshotResyncNeeded(
                            frameDelta > 0 ? ShooterClientResyncReason.FrameTooFarAhead : ShooterClientResyncReason.FrameTooFarBehind,
                            replayTargetFrame,
                            authoritativeFrame,
                            predictedHashBeforeCorrection,
                            authoritativeStateHash);
                    }
                    else
                    {
                        ClearFullSnapshotResync();
                        RecoveryState = wasAwaitingFullSnapshot && isStrongRecoverySnapshot ? ShooterClientRecoveryState.Recovered : ShooterClientRecoveryState.Normal;
                    }
                }
                else
                {
                    MarkFullSnapshotResyncNeeded(
                        ShooterClientResyncReason.AuthoritativeHashMismatch,
                        replayTargetFrame,
                        authoritativeFrame,
                        predictedHashBeforeCorrection,
                        authoritativeStateHash);
                }

                _presentation.PublishReconciliation(in reconciliation);
                PublishRuntimeSnapshot();
            }
            else
            {
                LastReconciliationResult = ShooterClientReconciliationResult.None;
                if (LastSnapshotApplyResult == ShooterSnapshotApplyResult.ImportFailed)
                {
                    MarkFullSnapshotResyncNeeded(
                        ShooterClientResyncReason.ImportFailed,
                        replayTargetFrame,
                        0,
                        predictedHashBeforeCorrection,
                        0u);
                }
            }

            return LastSnapshotApplyResult;
        }

        public ShooterClientFrameTickResult Tick(float deltaTime)
        {
            if (deltaTime < 0f) throw new ArgumentOutOfRangeException(nameof(deltaTime));

            if (!_runtime.IsStarted)
            {
                return ShooterClientFrameTickResult.NotStarted;
            }

            if (RecoveryState == ShooterClientRecoveryState.AwaitingFullSnapshot || RecoveryState == ShooterClientRecoveryState.ApplyingFullSnapshot)
            {
                _accumulator = 0f;
                return new ShooterClientFrameTickResult(0, _runtime.CurrentFrame, _runtime.ComputeStateHash());
            }

            if (RecoveryState == ShooterClientRecoveryState.CatchUp)
            {
                return TickCatchUp();
            }

            if (RecoveryState == ShooterClientRecoveryState.Recovered)
            {
                RecoveryState = ShooterClientRecoveryState.Normal;
            }

            _accumulator += deltaTime;
            var ticks = 0;
            while (_accumulator + 0.000001f >= _fixedDeltaTime)
            {
                if (!StepRuntimeFrameAndCapture())
                {
                    break;
                }

                ticks++;
                _accumulator -= _fixedDeltaTime;
            }

            if (ticks > 0)
            {
                PublishRuntimeSnapshot();
            }

            return new ShooterClientFrameTickResult(ticks, _runtime.CurrentFrame, _runtime.ComputeStateHash());
        }

        private bool StepRuntimeFrameAndCapture()
        {
            if (!StepRuntimeFrame())
            {
                return false;
            }

            CaptureRollbackSnapshot();
            return true;
        }

        private bool StepRuntimeFrame()
        {
            return _runtime.Tick(_fixedDeltaTime);
        }

        private void CaptureRollbackSnapshot()
        {
            if (_runtime.IsStarted)
            {
                _rollback.CaptureAndStore(new FrameIndex(_runtime.CurrentFrame));
            }
        }

        private ShooterClientFrameTickResult TickCatchUp()
        {
            var ticks = 0;
            while (_runtime.CurrentFrame < _catchUpTargetFrame && ticks < _recoveryPolicy.MaxCatchUpTicksPerUpdate)
            {
                if (!StepRuntimeFrameAndCapture())
                {
                    MarkFullSnapshotResyncNeeded(
                        ShooterClientResyncReason.FrameTooFarBehind,
                        _runtime.CurrentFrame,
                        _catchUpTargetFrame,
                        _runtime.ComputeStateHash(),
                        0u);
                    break;
                }

                ticks++;
            }

            _accumulator = 0f;
            if (_runtime.CurrentFrame >= _catchUpTargetFrame && RecoveryState == ShooterClientRecoveryState.CatchUp)
            {
                RecoveryState = ShooterClientRecoveryState.Normal;
                _catchUpTargetFrame = 0;
            }

            if (ticks > 0)
            {
                PublishRuntimeSnapshot();
            }

            return new ShooterClientFrameTickResult(ticks, _runtime.CurrentFrame, _runtime.ComputeStateHash());
        }

        private void RecordPendingInput(int frame, ShooterPlayerCommand[] commands)
        {
            _predictionReconciliation.RecordLocalInput(frame, commands);
        }

        public void MarkGatewayInputResyncRequested(int clientFrame, int authoritativeFrame, uint clientStateHash = 0u, uint authoritativeStateHash = 0u)
        {
            MarkFullSnapshotResyncNeeded(
                ShooterClientResyncReason.ClientHashRejectedByServer,
                clientFrame,
                authoritativeFrame,
                clientStateHash,
                authoritativeStateHash);
        }

        public bool TryEnterCatchUp(int authoritativeFrame)
        {
            if (!_runtime.IsStarted || authoritativeFrame <= _runtime.CurrentFrame)
            {
                return false;
            }

            var frameDelta = authoritativeFrame - _runtime.CurrentFrame;
            if (frameDelta > _recoveryPolicy.SmallCatchUpThreshold)
            {
                MarkFullSnapshotResyncNeeded(
                    ShooterClientResyncReason.FrameTooFarBehind,
                    _runtime.CurrentFrame,
                    authoritativeFrame,
                    _runtime.ComputeStateHash(),
                    0u);
                return false;
            }

            RecoveryState = ShooterClientRecoveryState.CatchUp;
            _catchUpTargetFrame = authoritativeFrame;
            return true;
        }

        private void MarkFullSnapshotResyncNeeded(
            ShooterClientResyncReason reason,
            int clientFrame,
            int authoritativeFrame,
            uint clientStateHash,
            uint authoritativeStateHash)
        {
            NeedsFullSnapshotResync = true;
            RecoveryState = ShooterClientRecoveryState.AwaitingFullSnapshot;
            LastResyncReason = reason;
            LastResyncClientFrame = clientFrame;
            LastResyncAuthoritativeFrame = authoritativeFrame;
            LastResyncClientStateHash = clientStateHash;
            LastResyncAuthoritativeStateHash = authoritativeStateHash;
        }

        private void ClearFullSnapshotResync()
        {
            NeedsFullSnapshotResync = false;
            LastResyncReason = ShooterClientResyncReason.None;
            LastResyncClientFrame = 0;
            LastResyncAuthoritativeFrame = 0;
            LastResyncClientStateHash = 0u;
            LastResyncAuthoritativeStateHash = 0u;
        }

        private static bool IsStrongRecoverySnapshot(uint snapshotFlags)
        {
            return (snapshotFlags & (ShooterPackedSnapshotFlags.Full | ShooterPackedSnapshotFlags.AuthorityOverride)) != 0u;
        }

        private void PublishRuntimeSnapshot()
        {
            _presentation.ApplyLocalPredictionSnapshot(_runtime.GetSnapshot());
        }
    }

    public enum ShooterClientResyncReason
    {
        None = 0,
        ImportFailed = 1,
        AuthoritativeHashMismatch = 2,
        ClientHashRejectedByServer = 3,
        FrameTooFarBehind = 4,
        FrameTooFarAhead = 5,
        SnapshotTimeout = 6,
        WorldMismatch = 7
    }

    public readonly struct ShooterClientFrameTickResult
    {
        public static readonly ShooterClientFrameTickResult NotStarted = new ShooterClientFrameTickResult(0, 0, 0u);

        public readonly int Ticks;
        public readonly int Frame;
        public readonly uint StateHash;

        public ShooterClientFrameTickResult(int ticks, int frame, uint stateHash)
        {
            Ticks = ticks;
            Frame = frame;
            StateHash = stateHash;
        }
    }

    public readonly struct ShooterClientReconciliationResult
    {
        public static readonly ShooterClientReconciliationResult None = new ShooterClientReconciliationResult(
            ShooterSnapshotApplyResult.Ignored,
            0,
            0u,
            0,
            0u,
            0u,
            false,
            0,
            0,
            0u,
            0,
            0,
            0);

        public readonly ShooterSnapshotApplyResult ApplyResult;
        public readonly int PredictedFrameBeforeCorrection;
        public readonly uint PredictedHashBeforeCorrection;
        public readonly int AuthoritativeFrame;
        public readonly uint AuthoritativeStateHash;
        public readonly uint ImportedStateHash;
        public readonly bool AuthoritativeHashMatched;
        public readonly int ReplayTicks;
        public readonly int FinalFrame;
        public readonly uint FinalStateHash;
        public readonly int PendingInputFramesBeforeCorrection;
        public readonly int PendingInputFramesAfterTrim;
        public readonly int PendingInputFramesAfterReplay;

        public ShooterClientReconciliationResult(
            ShooterSnapshotApplyResult applyResult,
            int predictedFrameBeforeCorrection,
            uint predictedHashBeforeCorrection,
            int authoritativeFrame,
            uint authoritativeStateHash,
            uint importedStateHash,
            bool authoritativeHashMatched,
            int replayTicks,
            int finalFrame,
            uint finalStateHash,
            int pendingInputFramesBeforeCorrection,
            int pendingInputFramesAfterTrim,
            int pendingInputFramesAfterReplay)
        {
            ApplyResult = applyResult;
            PredictedFrameBeforeCorrection = predictedFrameBeforeCorrection;
            PredictedHashBeforeCorrection = predictedHashBeforeCorrection;
            AuthoritativeFrame = authoritativeFrame;
            AuthoritativeStateHash = authoritativeStateHash;
            ImportedStateHash = importedStateHash;
            AuthoritativeHashMatched = authoritativeHashMatched;
            ReplayTicks = replayTicks;
            FinalFrame = finalFrame;
            FinalStateHash = finalStateHash;
            PendingInputFramesBeforeCorrection = pendingInputFramesBeforeCorrection;
            PendingInputFramesAfterTrim = pendingInputFramesAfterTrim;
            PendingInputFramesAfterReplay = pendingInputFramesAfterReplay;
        }

        public ShooterClientReconciliationResult(ShooterSnapshotApplyResult applyResult, in ClientPredictionReconciliationResult result)
            : this(
                applyResult,
                result.PredictedFrameBeforeCorrection,
                result.PredictedHashBeforeCorrection,
                result.AuthoritativeFrame,
                result.AuthoritativeStateHash,
                result.ImportedStateHash,
                result.AuthoritativeHashMatched,
                result.ReplayTicks,
                result.FinalFrame,
                result.FinalStateHash,
                result.PendingInputFramesBeforeCorrection,
                result.PendingInputFramesAfterTrim,
                result.PendingInputFramesAfterReplay)
        {
        }
    }
}
