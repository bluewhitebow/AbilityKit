using AbilityKit.Network.Runtime;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests;

public sealed class RemoteInterpolationPlaybackTests
{
    private sealed class TickSample : IRemoteSnapshotSample
    {
        public TickSample(long ticks, float value)
        {
            TimelineTicks = ticks;
            Value = value;
        }

        public long TimelineTicks { get; }

        public float Value { get; }
    }

    private static InterpolationConfig Config(long maxExtrapolationTicks = 50L)
    {
        // Snap-mode timeline (catchUpRate 0) keeps the estimate deterministic for assertions.
        return new InterpolationConfig(
            ticksPerSecond: 1000L,
            interpolationDelayTicks: 100L,
            bufferCapacity: 8,
            catchUpRate: 0d,
            maxExtrapolationTicks: maxExtrapolationTicks);
    }

    [Fact]
    public void TrySampleReturnsFalseUntilServerTimeObserved()
    {
        var playback = new RemoteInterpolationPlayback<TickSample>(Config());

        playback.Advance(0.1f);
        Assert.False(playback.TrySample(out _));
        Assert.False(playback.HasPublished);
    }

    [Fact]
    public void ObserveAdvanceAndSampleInterpolatesAtDelayedPlaybackTime()
    {
        var playback = new RemoteInterpolationPlayback<TickSample>(Config());

        Assert.True(playback.Observe(new TickSample(1000L, 0f)));
        Assert.True(playback.Observe(new TickSample(1100L, 10f)));

        // Estimate snaps to newest observed (1100), playback held 100 ticks behind -> 1000, the
        // oldest sample, so the result clamps to it before any advance.
        Assert.True(playback.TrySample(out var clamped));
        Assert.Equal(0f, clamped.From.Value);

        // Advance 50ms (50 ticks) -> estimate 1150, playback 1050, halfway between the two samples.
        playback.Advance(0.05f);
        Assert.True(playback.TrySample(out var interpolation));
        Assert.True(interpolation.IsInterpolating);
        Assert.Equal(0.5f, interpolation.Alpha, 3);
        Assert.True(playback.HasPublished);
        Assert.Equal(2, playback.BufferedSampleCount);
    }

    [Fact]
    public void ObserveRejectsStaleSampleWithoutAdvancingTimeline()
    {
        var playback = new RemoteInterpolationPlayback<TickSample>(Config());

        Assert.True(playback.Observe(new TickSample(2000L, 1f)));
        Assert.False(playback.Observe(new TickSample(1500L, 9f)));
        Assert.False(playback.Observe(new TickSample(2000L, 9f)));
        Assert.Equal(1, playback.BufferedSampleCount);
        Assert.Equal(2000L, playback.EstimatedServerTicks);
    }

    [Fact]
    public void StarvationFlaggedWhenPlaybackRunsPastNewestBeyondTolerance()
    {
        var playback = new RemoteInterpolationPlayback<TickSample>(Config(maxExtrapolationTicks: 50L));

        // Single sample at 1000; estimate snaps to 1000, playback held at 900 (delay 100), which is
        // before the only sample -> clamped, not starved.
        Assert.True(playback.Observe(new TickSample(1000L, 1f)));
        Assert.True(playback.TrySample(out _));
        Assert.False(playback.IsStarved);

        // Advance 200ms (200 ticks): estimate 1200, playback 1100, which is 100 ticks past the newest
        // sample (1000) -> beyond the 50-tick tolerance, so playback is flagged starved.
        playback.Advance(0.2f);
        Assert.True(playback.TrySample(out var interpolation));
        Assert.True(interpolation.IsExtrapolating);
        Assert.True(playback.IsStarved);
    }

    [Fact]
    public void GetDiagnosticsReflectsPlaybackState()
    {
        var playback = new RemoteInterpolationPlayback<TickSample>(Config());
        playback.Observe(new TickSample(1000L, 0f));
        playback.Observe(new TickSample(1100L, 10f));
        playback.Advance(0.05f);
        playback.TrySample(out _);

        var diagnostics = playback.GetDiagnostics();
        Assert.Equal(2, diagnostics.BufferedRemoteSnapshotCount);
        Assert.Equal(playback.PlaybackTicks, diagnostics.RemotePlaybackTicks);
        Assert.Equal(playback.EstimatedServerTicks, diagnostics.EstimatedServerTicks);
        Assert.True(diagnostics.HasPublishedRemoteFrame);
        Assert.Equal(100L, diagnostics.PlaybackDelayTicks);
    }

    [Fact]
    public void ResetClearsBufferTimelineAndFlags()
    {
        var playback = new RemoteInterpolationPlayback<TickSample>(Config());
        playback.Observe(new TickSample(1000L, 0f));
        playback.Observe(new TickSample(1100L, 10f));
        playback.Advance(0.05f);
        playback.TrySample(out _);

        playback.Reset();

        Assert.Equal(0, playback.BufferedSampleCount);
        Assert.False(playback.HasPublished);
        Assert.False(playback.IsStarved);
        Assert.Equal(0L, playback.EstimatedServerTicks);
        Assert.False(playback.TrySample(out _));
    }
}
