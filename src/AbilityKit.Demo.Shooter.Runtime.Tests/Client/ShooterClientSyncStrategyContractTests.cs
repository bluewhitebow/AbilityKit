using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Network.Runtime;
using AbilityKit.Network.Runtime.Sync;
using AbilityKit.Protocol.Shooter;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests;

/// <summary>
/// Verifies the migration-step-3 seam: both Shooter sync controllers are reachable through the
/// gameplay-agnostic framework contract <see cref="IClientSyncStrategy{TInput, TSample}"/>, and
/// the contract surface maps onto the same demo behaviour exposed by the concrete controllers.
/// </summary>
public sealed class ShooterClientSyncStrategyContractTests
{
    private static ShooterGatewaySnapshot RemoteSnapshot(int frame, long serverTicks, float actorX)
    {
        return new ShooterGatewaySnapshot(
            worldId: 9001ul,
            frame: frame,
            timestamp: 0d,
            serverTicks: serverTicks,
            isFullSnapshot: true,
            actors: new[]
            {
                new ShooterGatewayActorSnapshot(actorId: 7, x: actorX, y: 0f, rotation: 0f, velocityX: 0f, velocityY: 0f, hp: 100f, hpMax: 100f, teamId: 1)
            });
    }

    [Fact]
    public void PredictRollbackControllerIsReachableThroughFrameworkContract()
    {
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade();
        var controller = new ShooterClientPredictRollbackSyncController(
            runtime, presentation, tickRate: 30, decoder: null, gateway: null);

        IClientSyncStrategy<ShooterPlayerCommand, ShooterRemoteSnapshotSample> strategy = controller;

        Assert.Equal(NetworkSyncModel.PredictRollback, strategy.SyncModel);

        // Before any divergence the framework report is empty.
        var report = strategy.GetReconciliationReport();
        Assert.Equal(SyncReconciliationReason.None, report.Reason);
        Assert.Equal(SyncRecoveryState.Normal, report.RecoveryState);
        Assert.False(report.DidReconcile);

        // Ticking through the contract returns the same frame/tick the demo Tick produces.
        var contractTick = strategy.Tick(0f);
        Assert.Equal(controller.CurrentFrame, contractTick.Frame);
    }

    [Fact]
    public void InterpolationControllerObservesRemoteSamplesThroughFrameworkContract()
    {
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade();
        var config = new InterpolationConfig(ticksPerSecond: 1000L, interpolationDelayTicks: 100L, bufferCapacity: 16, catchUpRate: 0d);
        var controller = new ShooterClientAuthoritativeInterpolationSyncController(
            runtime, presentation, tickRate: 30, decoder: null, gateway: null, config);

        IClientSyncStrategy<ShooterPlayerCommand, ShooterRemoteSnapshotSample> strategy = controller;

        Assert.Equal(NetworkSyncModel.AuthoritativeInterpolation, strategy.SyncModel);

        // Feeding remote samples through the framework contract buffers them for delayed playback,
        // the same path BufferRemoteSnapshot uses.
        strategy.ObserveRemote(new ShooterRemoteSnapshotSample(9001ul, frame: 1, serverTicks: 1000L,
            actors: RemoteSnapshot(1, 1000L, 0f).Actors));
        strategy.ObserveRemote(new ShooterRemoteSnapshotSample(9001ul, frame: 2, serverTicks: 1100L,
            actors: RemoteSnapshot(2, 1100L, 10f).Actors));

        Assert.Equal(2, controller.BufferedRemoteSnapshotCount);
        Assert.Equal(1100L, controller.EstimatedServerTicks);

        // Interpolation never reconciles the local sim, so the framework report stays empty.
        var report = strategy.GetReconciliationReport();
        Assert.Equal(SyncReconciliationReason.None, report.Reason);
        Assert.False(report.DidReconcile);
    }
}
