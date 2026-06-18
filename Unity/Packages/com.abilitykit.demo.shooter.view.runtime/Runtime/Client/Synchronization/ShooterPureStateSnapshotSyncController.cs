using System;
using System.Collections.Generic;
using AbilityKit.Network.Runtime.Sync;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.View
{
    public sealed class ShooterPureStateSnapshotSyncController
    {
        private static readonly SyncHealthEvent[] EmptyHealthEvents = Array.Empty<SyncHealthEvent>();

        private readonly Action<ShooterPureStateSnapshotPayload> _applySnapshot;
        private readonly ShooterGatewaySnapshotDecoder _decoder;
        private SyncHealthEvent[] _lastHealthEvents = EmptyHealthEvents;
        private bool _hasAppliedSnapshot;

        public ShooterPureStateSnapshotSyncController(ShooterPresentationFacade presentation)
            : this(presentation, new ShooterGatewaySnapshotDecoder())
        {
        }

        public ShooterPureStateSnapshotSyncController(ShooterPresentationFacade presentation, ShooterGatewaySnapshotDecoder decoder)
            : this(snapshot => (presentation ?? throw new ArgumentNullException(nameof(presentation))).ApplyPureStateSnapshot(in snapshot), decoder)
        {
        }

        public ShooterPureStateSnapshotSyncController(Action<ShooterPureStateSnapshotPayload> applySnapshot, ShooterGatewaySnapshotDecoder decoder)
        {
            _applySnapshot = applySnapshot ?? throw new ArgumentNullException(nameof(applySnapshot));
            _decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
        }

        public int LastAppliedFrame { get; private set; }

        public uint LastAppliedStateHash { get; private set; }

        public int LastAppliedSnapshotKind { get; private set; }

        public int LastBaselineFrame { get; private set; }

        public uint LastBaselineHash { get; private set; }

        public bool NeedsFullBaselineResync { get; private set; }

        public IReadOnlyList<SyncHealthEvent> LastHealthEvents => _lastHealthEvents;

        public ShooterPureStateResyncReason LastResyncReason { get; private set; } = ShooterPureStateResyncReason.None;

        public int LastIgnoredFrame { get; private set; } = -1;

        public int LastResyncFrame { get; private set; }

        public uint LastResyncStateHash { get; private set; }

        public ShooterPureStateSyncDiagnostics LastDiagnostics { get; private set; }

        public ShooterPureStateSnapshotApplyResult TryApplyGatewayPush(uint opCode, ArraySegment<byte> payload)
        {
            if (!_decoder.IsSnapshotPush(opCode))
            {
                ClearHealthEvents();
                LastDiagnostics = ShooterPureStateSyncDiagnostics.Ignored(LastAppliedFrame, LastAppliedStateHash, NeedsFullBaselineResync, LastResyncReason);
                return ShooterPureStateSnapshotApplyResult.Ignored;
            }

            var snapshot = _decoder.Decode(payload);
            return ApplyGatewaySnapshot(in snapshot);
        }

        public ShooterPureStateSnapshotApplyResult ApplyGatewaySnapshot(in ShooterGatewaySnapshot snapshot)
        {
            if (!snapshot.PureStateSnapshot.HasValue)
            {
                ClearHealthEvents();
                LastDiagnostics = ShooterPureStateSyncDiagnostics.Ignored(LastAppliedFrame, LastAppliedStateHash, NeedsFullBaselineResync, LastResyncReason);
                return ShooterPureStateSnapshotApplyResult.Ignored;
            }

            var pureState = snapshot.PureStateSnapshot.Value;
            if (_hasAppliedSnapshot && pureState.Frame <= LastAppliedFrame)
            {
                LastIgnoredFrame = pureState.Frame;
                SetHealthEvents(SyncHealthEvent.Warning(SyncHealthEventKind.SnapshotStale, pureState.Frame, LastAppliedFrame));
                LastDiagnostics = ShooterPureStateSyncDiagnostics.FromSnapshot(
                    ShooterPureStateSnapshotApplyResult.IgnoredStaleSnapshot,
                    in pureState,
                    LastAppliedFrame,
                    LastAppliedStateHash,
                    NeedsFullBaselineResync,
                    LastResyncReason,
                    LastResyncFrame,
                    LastResyncStateHash,
                    LastIgnoredFrame);
                return ShooterPureStateSnapshotApplyResult.IgnoredStaleSnapshot;
            }

            var isFullBaseline = pureState.SnapshotKind == ShooterPureStateSnapshotKinds.FullBaseline;
            if (!isFullBaseline && !CanApplyDelta(in pureState))
            {
                NeedsFullBaselineResync = true;
                LastResyncFrame = pureState.Frame;
                LastResyncStateHash = pureState.StateHash;
                SetHealthEvents(SyncHealthEvent.Info(SyncHealthEventKind.FullSnapshotRequested, pureState.Frame, (long)LastResyncReason));
                LastDiagnostics = ShooterPureStateSyncDiagnostics.FromSnapshot(
                    ShooterPureStateSnapshotApplyResult.NeedsFullBaselineResync,
                    in pureState,
                    LastAppliedFrame,
                    LastAppliedStateHash,
                    NeedsFullBaselineResync,
                    LastResyncReason,
                    LastResyncFrame,
                    LastResyncStateHash,
                    LastIgnoredFrame);
                return ShooterPureStateSnapshotApplyResult.NeedsFullBaselineResync;
            }

            _applySnapshot(pureState);
            LastAppliedFrame = pureState.Frame;
            LastAppliedStateHash = pureState.StateHash;
            LastAppliedSnapshotKind = pureState.SnapshotKind;
            if (isFullBaseline)
            {
                LastBaselineFrame = pureState.BaselineFrame;
                LastBaselineHash = pureState.BaselineHash;
                NeedsFullBaselineResync = false;
                LastResyncReason = ShooterPureStateResyncReason.None;
                LastResyncFrame = 0;
                LastResyncStateHash = 0u;
                SetHealthEvents(
                    SyncHealthEvent.Info(SyncHealthEventKind.SnapshotReceived, pureState.Frame, pureState.Entities?.Length ?? 0),
                    SyncHealthEvent.Info(SyncHealthEventKind.FullSnapshotApplied, pureState.Frame, pureState.BaselineFrame));
            }
            else
            {
                SetHealthEvents(SyncHealthEvent.Info(SyncHealthEventKind.SnapshotReceived, pureState.Frame, pureState.Entities?.Length ?? 0));
            }

            _hasAppliedSnapshot = true;
            var result = isFullBaseline
                ? ShooterPureStateSnapshotApplyResult.AppliedFullBaseline
                : ShooterPureStateSnapshotApplyResult.AppliedDelta;
            LastDiagnostics = ShooterPureStateSyncDiagnostics.FromSnapshot(
                result,
                in pureState,
                LastAppliedFrame,
                LastAppliedStateHash,
                NeedsFullBaselineResync,
                LastResyncReason,
                LastResyncFrame,
                LastResyncStateHash,
                LastIgnoredFrame);
            return result;
        }

        private bool CanApplyDelta(in ShooterPureStateSnapshotPayload pureState)
        {
            if (!_hasAppliedSnapshot || LastBaselineFrame <= 0)
            {
                LastResyncReason = ShooterPureStateResyncReason.MissingBaseline;
                return false;
            }

            if (pureState.BaselineFrame != LastBaselineFrame || pureState.BaselineHash != LastBaselineHash)
            {
                LastResyncReason = ShooterPureStateResyncReason.BaselineMismatch;
                return false;
            }

            return true;
        }

        private void ClearHealthEvents()
        {
            _lastHealthEvents = EmptyHealthEvents;
        }

        private void SetHealthEvents(params SyncHealthEvent[] events)
        {
            _lastHealthEvents = events == null || events.Length == 0 ? EmptyHealthEvents : events;
        }
    }
    
    public readonly struct ShooterPureStateSyncDiagnostics
    {
        public ShooterPureStateSyncDiagnostics(
            ShooterPureStateSnapshotApplyResult lastApplyResult,
            int sourceFrame,
            int sourceSnapshotKind,
            int sourceEntityCount,
            int sourceVisibilityHintCount,
            int sourceBaselineFrame,
            uint sourceBaselineHash,
            uint sourceStateHash,
            long sourceServerTick,
            int appliedFrame,
            uint appliedStateHash,
            bool needsFullBaselineResync,
            ShooterPureStateResyncReason lastResyncReason,
            int lastResyncFrame,
            uint lastResyncStateHash,
            int lastIgnoredFrame)
        {
            LastApplyResult = lastApplyResult;
            SourceFrame = sourceFrame;
            SourceSnapshotKind = sourceSnapshotKind;
            SourceEntityCount = sourceEntityCount;
            SourceVisibilityHintCount = sourceVisibilityHintCount;
            SourceBaselineFrame = sourceBaselineFrame;
            SourceBaselineHash = sourceBaselineHash;
            SourceStateHash = sourceStateHash;
            SourceServerTick = sourceServerTick;
            AppliedFrame = appliedFrame;
            AppliedStateHash = appliedStateHash;
            NeedsFullBaselineResync = needsFullBaselineResync;
            LastResyncReason = lastResyncReason;
            LastResyncFrame = lastResyncFrame;
            LastResyncStateHash = lastResyncStateHash;
            LastIgnoredFrame = lastIgnoredFrame;
        }

        public ShooterPureStateSnapshotApplyResult LastApplyResult { get; }
        public int SourceFrame { get; }
        public int SourceSnapshotKind { get; }
        public int SourceEntityCount { get; }
        public int SourceVisibilityHintCount { get; }
        public int SourceBaselineFrame { get; }
        public uint SourceBaselineHash { get; }
        public uint SourceStateHash { get; }
        public long SourceServerTick { get; }
        public int AppliedFrame { get; }
        public uint AppliedStateHash { get; }
        public bool NeedsFullBaselineResync { get; }
        public ShooterPureStateResyncReason LastResyncReason { get; }
        public int LastResyncFrame { get; }
        public uint LastResyncStateHash { get; }
        public int LastIgnoredFrame { get; }
        public bool HasSourceSnapshot => SourceFrame > 0 || SourceEntityCount > 0 || SourceVisibilityHintCount > 0;
        public bool AppliedPresentation => LastApplyResult == ShooterPureStateSnapshotApplyResult.AppliedFullBaseline || LastApplyResult == ShooterPureStateSnapshotApplyResult.AppliedDelta;

        public static ShooterPureStateSyncDiagnostics Ignored(int appliedFrame, uint appliedStateHash, bool needsFullBaselineResync, ShooterPureStateResyncReason lastResyncReason)
        {
            return new ShooterPureStateSyncDiagnostics(
                ShooterPureStateSnapshotApplyResult.Ignored,
                0,
                0,
                0,
                0,
                0,
                0u,
                0u,
                0L,
                appliedFrame,
                appliedStateHash,
                needsFullBaselineResync,
                lastResyncReason,
                0,
                0u,
                -1);
        }

        public static ShooterPureStateSyncDiagnostics FromSnapshot(
            ShooterPureStateSnapshotApplyResult result,
            in ShooterPureStateSnapshotPayload snapshot,
            int appliedFrame,
            uint appliedStateHash,
            bool needsFullBaselineResync,
            ShooterPureStateResyncReason lastResyncReason,
            int lastResyncFrame,
            uint lastResyncStateHash,
            int lastIgnoredFrame)
        {
            return new ShooterPureStateSyncDiagnostics(
                result,
                snapshot.Frame,
                snapshot.SnapshotKind,
                snapshot.Entities?.Length ?? 0,
                snapshot.VisibilityHints?.Length ?? 0,
                snapshot.BaselineFrame,
                snapshot.BaselineHash,
                snapshot.StateHash,
                snapshot.ServerTick,
                appliedFrame,
                appliedStateHash,
                needsFullBaselineResync,
                lastResyncReason,
                lastResyncFrame,
                lastResyncStateHash,
                lastIgnoredFrame);
        }
    }

    public enum ShooterPureStateSnapshotApplyResult
    {
        Ignored = 0,
        AppliedFullBaseline = 1,
        AppliedDelta = 2,
        IgnoredStaleSnapshot = 3,
        NeedsFullBaselineResync = 4
    }

    public enum ShooterPureStateResyncReason
    {
        None = 0,
        MissingBaseline = 1,
        BaselineMismatch = 2
    }
}
