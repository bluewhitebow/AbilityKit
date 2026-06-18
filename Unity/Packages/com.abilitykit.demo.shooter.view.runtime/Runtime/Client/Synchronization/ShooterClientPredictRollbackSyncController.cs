#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Network.Runtime;
using AbilityKit.Network.Runtime.Sync;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.View
{
    /// <summary>
    /// <see cref="NetworkSyncModel.PredictRollback"/> 客户端控制器。
    /// 将现有本地预测、权威快照、回滚与重放链路
    /// （<see cref="ShooterClientFrameSyncCoordinator"/> + <see cref="ShooterClientInputCoordinator"/>）
    /// 包装到通用 <see cref="IShooterClientSyncController"/> 接缝之后，让会话无需了解当前同步模型即可委托执行。
    /// </summary>
    public sealed class ShooterClientPredictRollbackSyncController : IShooterClientSyncController
    {
        private readonly ShooterClientFrameSyncCoordinator _frameSync;
        private readonly ShooterClientInputCoordinator _input;

        public ShooterClientPredictRollbackSyncController(
            IShooterBattleRuntimePort runtime,
            ShooterPresentationFacade presentation,
            int tickRate,
            ShooterGatewaySnapshotDecoder? decoder,
            IShooterRoomGatewayClient? gateway)
        {
            _frameSync = new ShooterClientFrameSyncCoordinator(runtime, presentation, tickRate, decoder);
            _input = new ShooterClientInputCoordinator(_frameSync, gateway);
        }

        public NetworkSyncModel SyncModel => NetworkSyncModel.PredictRollback;

        public bool IsStarted => _frameSync.IsStarted;

        public int CurrentFrame => _frameSync.CurrentFrame;

        public ShooterClientFrameSyncController FrameSync => _frameSync.Controller;

        public ShooterClientFrameSyncCoordinator FrameSyncCoordinator => _frameSync;

        public ShooterClientInputCoordinator InputCoordinator => _input;

        public ShooterClientReconciliationResult LastReconciliationResult => _frameSync.LastReconciliationResult;

        public bool NeedsFullSnapshotResync => _frameSync.NeedsFullSnapshotResync;

        public ShooterClientRecoveryState RecoveryState => _frameSync.RecoveryState;

        public AbilityKit.Network.Runtime.Sync.FastReconnectPhase FastReconnectPhase => _frameSync.FastReconnectPhase;

        public IReadOnlyList<SyncHealthEvent> LastFastReconnectHealthEvents
            => MergeHealthEvents(_frameSync.LastFastReconnectHealthEvents, _input.LastHealthEvents);

        public ShooterClientResyncReason LastResyncReason => _frameSync.LastResyncReason;

        public int LastResyncClientFrame => _frameSync.LastResyncClientFrame;

        public int LastResyncAuthoritativeFrame => _frameSync.LastResyncAuthoritativeFrame;

        public uint LastResyncClientStateHash => _frameSync.LastResyncClientStateHash;

        public uint LastResyncAuthoritativeStateHash => _frameSync.LastResyncAuthoritativeStateHash;

        public bool HasGateway => _input.HasGateway;

        public bool StartGame(in ShooterStartGamePayload startGame)
        {
            return _frameSync.StartGame(in startGame);
        }

        public ShooterClientInputSubmitResult SubmitLocalInput(int playerId, float moveX, float moveY, float aimX, float aimY, bool fire)
        {
            return _input.SubmitLocalInput(playerId, moveX, moveY, aimX, aimY, fire);
        }

        public ShooterClientInputSubmitResult SubmitLocalInput(in ShooterPlayerCommand command)
        {
            return _input.SubmitLocalInput(in command);
        }

        public Task<ShooterClientGatewayInputSubmitResult> SubmitLocalInputToGatewayAsync(
            ShooterGatewayBattleInputContext context,
            ShooterPlayerCommand command,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            return _input.SubmitLocalInputToGatewayAsync(context, command, timeout, cancellationToken);
        }

        public Task<ShooterClientGatewayInputSubmitResult> SubmitAcceptedInputToGatewayAsync(
            ShooterGatewayBattleInputContext context,
            ShooterClientInputSubmitResult local,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            return _input.SubmitAcceptedInputToGatewayAsync(context, local, timeout, cancellationToken);
        }

        public ShooterClientFrameTickResult Tick(float deltaTime)
        {
            return _frameSync.Tick(deltaTime);
        }

        public ShooterClientFrameTickResult CatchUpToFrame(int targetFrame)
        {
            return _frameSync.CatchUpToFrame(targetFrame);
        }

        public bool TryEnterCatchUp(int authoritativeFrame)
        {
            return _frameSync.TryEnterCatchUp(authoritativeFrame);
        }

        public ShooterSnapshotApplyResult ApplyGatewayPush(uint opCode, ArraySegment<byte> payload)
        {
            return _frameSync.ApplyGatewayPush(opCode, payload);
        }

        private static IReadOnlyList<SyncHealthEvent> MergeHealthEvents(
            IReadOnlyList<SyncHealthEvent> primary,
            IReadOnlyList<SyncHealthEvent> secondary)
        {
            if (primary.Count == 0)
            {
                return secondary;
            }

            if (secondary.Count == 0)
            {
                return primary;
            }

            var merged = new SyncHealthEvent[primary.Count + secondary.Count];
            for (int i = 0; i < primary.Count; i++)
            {
                merged[i] = primary[i];
            }

            for (int i = 0; i < secondary.Count; i++)
            {
                merged[primary.Count + i] = secondary[i];
            }

            return merged;
        }

        // --- IClientSyncStrategy<ShooterPlayerCommand, ShooterRemoteSnapshotSample> ---
        // 显式框架契约接口，映射到现有示例行为。

        SyncTickResult IClientSyncStrategy<ShooterPlayerCommand, ShooterRemoteSnapshotSample>.Tick(float deltaSeconds)
        {
            return ShooterClientSyncStrategyMapping.ToSyncTickResult(Tick(deltaSeconds));
        }

        void IClientSyncStrategy<ShooterPlayerCommand, ShooterRemoteSnapshotSample>.SubmitInput(in ShooterPlayerCommand input)
        {
            SubmitLocalInput(in input);
        }

        void IClientSyncStrategy<ShooterPlayerCommand, ShooterRemoteSnapshotSample>.ObserveRemote(in ShooterRemoteSnapshotSample sample)
        {
            // 预测回滚不消费逐 actor 远端样本：它通过 ApplyGatewayPush 导入打包权威快照字节，
            // 再回滚/前滚本地模拟完成校正。因此对该模型来说，观察已解码样本是空操作。
        }

        SyncReconciliationReport IClientSyncStrategy<ShooterPlayerCommand, ShooterRemoteSnapshotSample>.GetReconciliationReport()
        {
            return ShooterClientSyncStrategyMapping.ToReconciliationReport(this);
        }
    }
}
