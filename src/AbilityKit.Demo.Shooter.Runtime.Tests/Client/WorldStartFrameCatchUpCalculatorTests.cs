using AbilityKit.Ability.Host.Extensions.Client.FrameSync;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests.Client;

public sealed class WorldStartFrameCatchUpCalculatorTests
{
    [Fact]
    public void CalculateReturnsTargetFrameFromWorldStartAnchor()
    {
        var anchor = new WorldStartFrameAnchor(200000L, 10000000L, 30, 1d / 30d);

        var result = WorldStartFrameCatchUpCalculator.Calculate(in anchor, 1200000L);

        Assert.True(result.AnchorValid);
        Assert.Equal(33, result.TargetFrame);
        Assert.Equal(3, result.CatchUpFrames);
        Assert.Equal(0.1d, result.ElapsedSeconds, precision: 6);
    }

    [Fact]
    public void CalculateClampsToStartFrameWhenServerTimeIsBeforeAnchor()
    {
        var anchor = new WorldStartFrameAnchor(200000L, 10000000L, 30, 1d / 30d);

        var result = WorldStartFrameCatchUpCalculator.Calculate(in anchor, 100000L);

        Assert.True(result.AnchorValid);
        Assert.Equal(30, result.TargetFrame);
        Assert.Equal(0, result.CatchUpFrames);
        Assert.Equal(0d, result.ElapsedSeconds);
    }

    [Fact]
    public void CalculateFromSnapshotFrameUsesSnapshotFrameAsCatchUpBase()
    {
        var anchor = new WorldStartFrameAnchor(123456L, 10000000L, 18, 1d / 30d);

        var result = WorldStartFrameCatchUpCalculator.CalculateFromSnapshotFrame(in anchor, 1123456L, snapshotFrame: 20);

        Assert.True(result.AnchorValid);
        Assert.Equal(21, result.TargetFrame);
        Assert.Equal(1, result.CatchUpFrames);
    }
}
