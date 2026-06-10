using System;
using AbilityKit.Protocol.Shooter;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests.Protocol;

public sealed class ShooterStartGameCodecTests
{
    [Fact]
    public void RoundTripPreservesWorldStartAnchor()
    {
        var payload = new ShooterStartGamePayload(
            "codec-world-anchor",
            60,
            7102,
            new[]
            {
                new ShooterStartPlayer(1, "P1", 1f, 2f),
                new ShooterStartPlayer(2, "P2", 3f, 4f)
            },
            worldId: 9001ul,
            startServerTicks: 123456789L,
            serverTickFrequency: TimeSpan.TicksPerSecond,
            startFrame: 12,
            fixedDeltaSeconds: 1d / 60d);

        var bytes = ShooterStartGameCodec.Serialize(in payload);
        var restored = ShooterStartGameCodec.Deserialize(bytes);

        Assert.Equal(payload.MatchId, restored.MatchId);
        Assert.Equal(payload.TickRate, restored.TickRate);
        Assert.Equal(payload.RandomSeed, restored.RandomSeed);
        Assert.Equal(payload.WorldId, restored.WorldId);
        Assert.Equal(payload.StartServerTicks, restored.StartServerTicks);
        Assert.Equal(payload.ServerTickFrequency, restored.ServerTickFrequency);
        Assert.Equal(payload.StartFrame, restored.StartFrame);
        Assert.Equal(payload.FixedDeltaSeconds, restored.FixedDeltaSeconds);
        Assert.True(restored.HasWorldStartAnchor);
        Assert.Equal(2, restored.Players.Length);
        Assert.Equal(2, restored.Players[1].PlayerId);
    }

    [Fact]
    public void EmptyPayloadUsesDefaultStartSpecWithoutAnchor()
    {
        var restored = ShooterStartGameCodec.Deserialize(Array.Empty<byte>());

        Assert.Equal(string.Empty, restored.MatchId);
        Assert.Equal(30, restored.TickRate);
        Assert.Equal(0, restored.RandomSeed);
        Assert.Empty(restored.Players);
        Assert.Equal(0ul, restored.WorldId);
        Assert.False(restored.HasWorldStartAnchor);
    }
}
