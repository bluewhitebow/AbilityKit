using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Game.Battle.Agent;
using AbilityKit.Network.Runtime;
using AbilityKit.Network.Runtime.Sync;
using Xunit;

namespace AbilityKit.Demo.Moba.View.Runtime.Tests;

/// <summary>
/// Validates that the Moba demo is reachable through the gameplay-agnostic framework contract
/// <see cref="IClientSyncStrategy{TInput, TSample}"/> — the same A-axis seam Shooter adopts. This proves
/// the abstraction is genuinely demo-agnostic: a second carrier (Moba, 3D, different input command and
/// sample type) drives the identical Tick / SubmitInput / ObserveRemote / GetReconciliationReport surface.
/// </summary>
public sealed class MobaClientSyncStrategyContractTests
{
    // 1 tick == 1 frame; delay of 1 frame so two samples bracket the playback time.
    private static InterpolationConfig Config() =>
        new InterpolationConfig(
            ticksPerSecond: 1L,
            interpolationDelayTicks: 1L,
            bufferCapacity: 16,
            catchUpRate: 0d);

    private static MobaRemoteSnapshotSample Sample(int frame, float x, float z)
    {
        var actors = new[]
        {
            new GatewayStateSyncActorSnapshot(
                actorId: 1,
                x: x,
                y: 0f,
                z: z,
                rotation: 0f,
                velocityX: 0f,
                velocityZ: 0f,
                hp: 100f,
                hpMax: 100f,
                teamId: 1),
        };

        return new MobaRemoteSnapshotSample(worldId: 7UL, frame: frame, actors: actors);
    }

    [Fact]
    public void ControllerIsReachableThroughFrameworkContract()
    {
        var controller = new MobaClientAuthoritativeInterpolationSyncController(Config());

        IClientSyncStrategy<PlayerInputCommand, MobaRemoteSnapshotSample> strategy = controller;

        Assert.Equal(NetworkSyncModel.AuthoritativeInterpolation, strategy.SyncModel);
        Assert.False(strategy.IsStarted);

        // Authoritative interpolation never reconciles; the report is always None.
        var report = strategy.GetReconciliationReport();
        Assert.Equal(SyncReconciliationReason.None, report.Reason);
        Assert.Equal(SyncRecoveryState.Normal, report.RecoveryState);
        Assert.False(report.DidReconcile);

        // SubmitInput is a no-op for this presentation strategy and must not throw.
        var command = new PlayerInputCommand(new FrameIndex(0), new PlayerId("p1"), opCode: 0, payload: System.Array.Empty<byte>());
        strategy.SubmitInput(in command);
    }

    [Fact]
    public void ObservesRemoteSamplesThroughFrameworkContract()
    {
        var controller = new MobaClientAuthoritativeInterpolationSyncController(Config());

        IClientSyncStrategy<PlayerInputCommand, MobaRemoteSnapshotSample> strategy = controller;

        strategy.ObserveRemote(Sample(frame: 0, x: 0f, z: 0f));
        strategy.ObserveRemote(Sample(frame: 2, x: 10f, z: 20f));

        Assert.Equal(2, controller.BufferedRemoteSnapshotCount);

        // Ticking through the contract advances the delayed playback and projects an interpolated frame.
        var tick = strategy.Tick(1f);

        Assert.True(strategy.IsStarted);
        Assert.True(controller.HasPublishedRemoteFrame);
        Assert.Equal(tick.Frame, controller.CurrentFrame);
        Assert.Equal(SyncReconciliationReason.None, strategy.GetReconciliationReport().Reason);
    }
}
