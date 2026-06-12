using System;
using System.Collections.Generic;
using AbilityKit.Network.Protocol;
using AbilityKit.Network.Runtime.Conditioning;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests;

/// <summary>
/// Deterministic, clock-driven coverage for the B-axis network environment simulator.
/// All tests use a virtual clock plus a fixed seed so timing, loss and reorder behaviour are
/// fully replayable without real waiting.
/// </summary>
public sealed class NetworkConditioningMiddlewareTests
{
    private sealed class DeliveryLog
    {
        public readonly List<uint> Sequences = new();

        public Action<NetworkPacketHeader, ArraySegment<byte>> Next => (header, _) => Sequences.Add(header.Seq);
    }

    private static NetworkPacketHeader Header(uint seq, byte[] payload)
    {
        return new NetworkPacketHeader(NetworkPacketFlags.None, opCode: 1u, seq: seq, payloadLength: (uint)payload.Length);
    }

    private static void Inbound(NetworkConditioningMiddleware mw, uint seq, DeliveryLog log)
    {
        var payload = new byte[] { (byte)seq };
        mw.OnInbound(null!, Header(seq, payload), new ArraySegment<byte>(payload), log.Next);
    }

    [Fact]
    public void IdealProfileDeliversImmediatelyOnAdvance()
    {
        long now = 0;
        var mw = new NetworkConditioningMiddleware(NetworkConditionProfile.Ideal, () => now, seed: 1);
        var log = new DeliveryLog();

        Inbound(mw, 1u, log);

        // Nothing is released until Advance is called.
        Assert.Empty(log.Sequences);

        mw.Advance(now);
        Assert.Equal(new uint[] { 1u }, log.Sequences);

        var stats = mw.GetStats();
        Assert.Equal(1, stats.InboundReceived);
        Assert.Equal(1, stats.InboundDelivered);
        Assert.Equal(0, stats.InboundDropped);
        Assert.Equal(0, stats.PendingCount);
    }

    [Fact]
    public void BaseLatencyHoldsPacketUntilDeliveryTime()
    {
        long now = 0;
        // Pure latency, no jitter/loss/reorder so the delivery time is exactly BaseLatencyMs.
        var profile = new NetworkConditionProfile(baseLatencyMs: 100, jitterMs: 0, packetLossRate: 0, reorderRate: 0, bandwidthKbps: 0);
        var mw = new NetworkConditioningMiddleware(profile, () => now, seed: 1);
        var log = new DeliveryLog();

        Inbound(mw, 7u, log);

        now = 99;
        mw.Advance(now);
        Assert.Empty(log.Sequences);
        Assert.Equal(1, mw.GetStats().PendingCount);

        now = 100;
        mw.Advance(now);
        Assert.Equal(new uint[] { 7u }, log.Sequences);
        Assert.Equal(0, mw.GetStats().PendingCount);
    }

    [Fact]
    public void PacketLossDropsPacketsAndCountsThem()
    {
        long now = 0;
        // 100% loss removes everything; counters still record receipt and drop.
        var profile = new NetworkConditionProfile(baseLatencyMs: 0, jitterMs: 0, packetLossRate: 1d, reorderRate: 0, bandwidthKbps: 0);
        var mw = new NetworkConditioningMiddleware(profile, () => now, seed: 1);
        var log = new DeliveryLog();

        Inbound(mw, 1u, log);
        Inbound(mw, 2u, log);
        Inbound(mw, 3u, log);

        mw.Advance(now);

        Assert.Empty(log.Sequences);
        var stats = mw.GetStats();
        Assert.Equal(3, stats.InboundReceived);
        Assert.Equal(3, stats.InboundDropped);
        Assert.Equal(0, stats.InboundDelivered);
        Assert.Equal(0, stats.PendingCount);
    }

    [Fact]
    public void PayloadIsCopiedSoCallerBufferReuseIsSafe()
    {
        long now = 0;
        var profile = new NetworkConditionProfile(baseLatencyMs: 50, jitterMs: 0, packetLossRate: 0, reorderRate: 0, bandwidthKbps: 0);
        var mw = new NetworkConditioningMiddleware(profile, () => now, seed: 1);

        byte[]? delivered = null;
        Action<NetworkPacketHeader, ArraySegment<byte>> capture = (_, seg) =>
        {
            delivered = new byte[seg.Count];
            Array.Copy(seg.Array!, seg.Offset, delivered, 0, seg.Count);
        };

        var buffer = new byte[] { 42 };
        mw.OnInbound(null!, Header(1u, buffer), new ArraySegment<byte>(buffer), capture);

        // Mutate the caller buffer after scheduling; the buffered copy must be unaffected.
        buffer[0] = 99;

        now = 50;
        mw.Advance(now);

        Assert.NotNull(delivered);
        Assert.Equal(42, delivered![0]);
    }

    [Fact]
    public void ReorderedPacketOvertakesAnEarlierInOrderNeighbour()
    {
        long now = 0;
        // High base latency with no jitter keeps in-order packets at a fixed delivery time, while a
        // reordered packet is pulled forward ahead of them. We enqueue several packets in one batch
        // and assert the delivered order is no longer strictly increasing (an inversion occurred),
        // which is the observable signature of reordering. The fixed seed makes this reproducible.
        var profile = new NetworkConditionProfile(baseLatencyMs: 100, jitterMs: 0, packetLossRate: 0, reorderRate: 0.5d, bandwidthKbps: 0);
        var mw = new NetworkConditioningMiddleware(profile, () => now, seed: 7);
        var log = new DeliveryLog();

        for (uint seq = 0; seq < 12; seq++)
        {
            Inbound(mw, seq, log);
        }

        // Flush everything (reordered packets at delay 0, in-order packets at delay 100).
        now = 100;
        mw.Advance(now);

        var stats = mw.GetStats();
        Assert.Equal(12, stats.InboundReceived);
        Assert.Equal(12, stats.InboundDelivered);
        Assert.True(stats.InboundReordered > 0, "expected at least one packet to be reordered");

        // All packets delivered, but not in arrival order: at least one later seq precedes an earlier.
        Assert.Equal(12, log.Sequences.Count);
        Assert.True(HasInversion(log.Sequences), "expected delivered order to contain an inversion");
    }

    private static bool HasInversion(IReadOnlyList<uint> sequences)
    {
        for (int i = 1; i < sequences.Count; i++)
        {
            if (sequences[i] < sequences[i - 1])
            {
                return true;
            }
        }

        return false;
    }

    [Fact]
    public void DeterministicAcrossRunsWithSameSeed()
    {
        var profile = NetworkConditionProfile.Mobile4G;

        var first = RunSequence(profile, seed: 12345);
        var second = RunSequence(profile, seed: 12345);

        Assert.Equal(first, second);
    }

    private static List<uint> RunSequence(NetworkConditionProfile profile, int seed)
    {
        long now = 0;
        var mw = new NetworkConditioningMiddleware(profile, () => now, seed);
        var log = new DeliveryLog();

        for (uint seq = 0; seq < 50; seq++)
        {
            Inbound(mw, seq, log);
            now += 10;
            mw.Advance(now);
        }

        // Drain anything still buffered far in the future.
        now += 10_000;
        mw.Advance(now);

        return log.Sequences;
    }
}
