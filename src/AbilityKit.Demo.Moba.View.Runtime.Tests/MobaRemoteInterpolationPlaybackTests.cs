using AbilityKit.Game.Battle.Agent;
using AbilityKit.Network.Runtime;
using Xunit;

namespace AbilityKit.Demo.Moba.View.Runtime.Tests;

/// <summary>
/// Validates that the Moba demo genuinely reuses the framework <see cref="RemoteInterpolationPlayback{TSample}"/>
/// via its thin <see cref="MobaRemoteInterpolationPlayback"/> adapter: buffering, the delayed playback
/// timeline, 3D actor interpolation, and the shared starvation policy all flow through the framework while
/// Moba only supplies its own sample + projector.
/// </summary>
public sealed class MobaRemoteInterpolationPlaybackTests
{
    // 1 tick == 1 frame; delay of 1 frame so two samples bracket the playback time.
    private static InterpolationConfig Config(long maxExtrapolationTicks = 50L) =>
        new InterpolationConfig(
            ticksPerSecond: 1L,
            interpolationDelayTicks: 1L,
            bufferCapacity: 16,
            catchUpRate: 0d,
            maxExtrapolationTicks: maxExtrapolationTicks);

    private static GatewayStateSyncSnapshot Snapshot(int frame, float x, float z)
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

        return new GatewayStateSyncSnapshot(worldId: 7UL, frame: frame, timestamp: 0d, isFullSnapshot: true, actors: actors);
    }

    [Fact]
    public void TryProjectRemoteFrame_ReturnsFalse_BeforeAnySnapshotObserved()
    {
        var playback = new MobaRemoteInterpolationPlayback(Config());

        playback.Advance(1f);

        Assert.False(playback.TryProjectRemoteFrame(out _));
        Assert.False(playback.HasPublishedRemoteFrame);
        Assert.Equal(0, playback.BufferedRemoteSnapshotCount);
    }

    [Fact]
    public void Observe_RejectsStaleSnapshot()
    {
        var playback = new MobaRemoteInterpolationPlayback(Config());

        Assert.True(playback.Observe(Snapshot(frame: 10, x: 0f, z: 0f)));
        Assert.False(playback.Observe(Snapshot(frame: 10, x: 5f, z: 5f)));
        Assert.Equal(1, playback.BufferedRemoteSnapshotCount);
    }

    [Fact]
    public void TryProjectRemoteFrame_InterpolatesActorPositionInThreeD()
    {
        var playback = new MobaRemoteInterpolationPlayback(Config());

        playback.Observe(Snapshot(frame: 0, x: 0f, z: 0f));
        playback.Observe(Snapshot(frame: 2, x: 10f, z: 20f));

        // The estimate snaps to the newest observed frame (2); with a 1-frame delay the delayed playback
        // time settles at frame 1, exactly the midpoint between the two buffered samples.
        Assert.True(playback.TryProjectRemoteFrame(out var snapshot));
        Assert.True(playback.HasPublishedRemoteFrame);

        var actor = Assert.Single(snapshot.Actors);
        Assert.Equal(5f, actor.X, precision: 3);
        Assert.Equal(0f, actor.Y, precision: 3); // Y stays 0 in both samples.
        Assert.Equal(10f, actor.Z, precision: 3);
    }

    [Fact]
    public void TryProjectRemoteFrame_FlagsStarvation_WhenPlaybackRunsPastNewestSample()
    {
        var playback = new MobaRemoteInterpolationPlayback(Config(maxExtrapolationTicks: 1L));

        playback.Observe(Snapshot(frame: 0, x: 0f, z: 0f));
        playback.Observe(Snapshot(frame: 2, x: 10f, z: 20f));

        // Advance far beyond the newest buffered sample (frame 2) so the delayed playback time runs past
        // it by more than the extrapolation tolerance.
        playback.Advance(100f);

        Assert.True(playback.TryProjectRemoteFrame(out var snapshot));
        Assert.True(playback.IsRemotePlaybackStarved);

        // Starved playback holds the newest authoritative pose rather than extrapolating further.
        var actor = Assert.Single(snapshot.Actors);
        Assert.Equal(10f, actor.X, precision: 3);
        Assert.Equal(20f, actor.Z, precision: 3);
    }

    [Fact]
    public void Reset_ClearsBufferAndPlaybackState()
    {
        var playback = new MobaRemoteInterpolationPlayback(Config());

        playback.Observe(Snapshot(frame: 0, x: 0f, z: 0f));
        playback.Observe(Snapshot(frame: 2, x: 10f, z: 20f));
        playback.Advance(2f);
        Assert.True(playback.TryProjectRemoteFrame(out _));

        playback.Reset();

        Assert.Equal(0, playback.BufferedRemoteSnapshotCount);
        Assert.False(playback.HasPublishedRemoteFrame);
        Assert.False(playback.IsRemotePlaybackStarved);
        Assert.False(playback.TryProjectRemoteFrame(out _));
    }

    [Fact]
    public void GetInterpolationDiagnostics_ReflectsFrameworkPlaybackState()
    {
        var playback = new MobaRemoteInterpolationPlayback(Config());

        playback.Observe(Snapshot(frame: 0, x: 0f, z: 0f));
        playback.Observe(Snapshot(frame: 2, x: 10f, z: 20f));
        playback.Advance(2f);
        playback.TryProjectRemoteFrame(out _);

        var diagnostics = playback.GetInterpolationDiagnostics();

        Assert.Equal(2, diagnostics.BufferedRemoteSnapshotCount);
        Assert.True(diagnostics.HasPublishedRemoteFrame);
    }
}
