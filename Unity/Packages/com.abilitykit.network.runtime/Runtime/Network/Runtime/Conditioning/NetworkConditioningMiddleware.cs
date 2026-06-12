#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using AbilityKit.Network.Abstractions;
using AbilityKit.Network.Protocol;

namespace AbilityKit.Network.Runtime.Conditioning
{
    /// <summary>
    /// A reproducible network environment simulator that plugs into the middleware chain.
    /// It applies a <see cref="NetworkConditionProfile"/> (latency, jitter, loss, reorder) to both
    /// inbound and outbound packets so any sync model can be exercised under controlled, repeatable
    /// adverse conditions.
    ///
    /// Delivery is time-driven and deterministic: a packet that is not dropped is buffered with a
    /// scheduled delivery time, and is released to the next stage only when <see cref="Advance"/> is
    /// called with a clock value at or past that time. Supplying a fixed <paramref name="seed"/> and an
    /// injectable clock makes "sync model x network profile" comparisons fully replayable in tests
    /// without real waiting.
    /// </summary>
    public sealed class NetworkConditioningMiddleware : INetworkMiddleware
    {
        private sealed class PendingPacket
        {
            public long DeliverAtMs;
            public long Sequence;
            public bool Inbound;
            public NetworkPacketHeader Header;
            public byte[] Payload;
            public Action<NetworkPacketHeader, ArraySegment<byte>> Next;
        }

        private readonly NetworkConditionProfile _profile;
        private readonly Func<long> _clockMs;
        private readonly Random _random;
        private readonly List<PendingPacket> _pending = new List<PendingPacket>();

        private long _enqueueCounter;

        private long _inboundReceived;
        private long _inboundDelivered;
        private long _inboundDropped;
        private long _inboundReordered;
        private long _outboundReceived;
        private long _outboundDelivered;
        private long _outboundDropped;
        private long _outboundReordered;

        /// <summary>
        /// Creates a conditioning middleware.
        /// </summary>
        /// <param name="profile">The network conditions to apply.</param>
        /// <param name="clockMs">
        /// Monotonic clock returning the current time in milliseconds. Injectable so tests can drive a
        /// virtual clock. When null, a real wall clock is used.
        /// </param>
        /// <param name="seed">Seed for the deterministic random source backing jitter, loss and reorder.</param>
        public NetworkConditioningMiddleware(NetworkConditionProfile profile, Func<long>? clockMs = null, int seed = 0)
        {
            _profile = profile;
            _clockMs = clockMs ?? DefaultClock;
            _random = new Random(seed);
        }

        public void OnInbound(ISessionContext context, NetworkPacketHeader header, ArraySegment<byte> payload, Action<NetworkPacketHeader, ArraySegment<byte>> next)
        {
            _inboundReceived++;
            Schedule(inbound: true, header, payload, next);
        }

        public void OnOutbound(ISessionContext context, NetworkPacketHeader header, ArraySegment<byte> payload, Action<NetworkPacketHeader, ArraySegment<byte>> next)
        {
            _outboundReceived++;
            Schedule(inbound: false, header, payload, next);
        }

        /// <summary>
        /// Releases every buffered packet whose scheduled delivery time is at or before
        /// <paramref name="nowMs"/>, in delivery-time order. Call this from the host loop (or a test)
        /// with the current clock value to flush due packets.
        /// </summary>
        public void Advance(long nowMs)
        {
            // Stable order: by delivery time, then by original enqueue sequence so equal times keep
            // arrival order unless explicitly reordered at schedule time.
            _pending.Sort(static (a, b) =>
            {
                int byTime = a.DeliverAtMs.CompareTo(b.DeliverAtMs);
                return byTime != 0 ? byTime : a.Sequence.CompareTo(b.Sequence);
            });

            int i = 0;
            while (i < _pending.Count && _pending[i].DeliverAtMs <= nowMs)
            {
                var packet = _pending[i];
                _pending.RemoveAt(i);

                if (packet.Inbound) _inboundDelivered++;
                else _outboundDelivered++;

                packet.Next(packet.Header, new ArraySegment<byte>(packet.Payload));
            }
        }

        public NetworkConditioningStats GetStats()
        {
            return new NetworkConditioningStats(
                _inboundReceived,
                _inboundDelivered,
                _inboundDropped,
                _inboundReordered,
                _outboundReceived,
                _outboundDelivered,
                _outboundDropped,
                _outboundReordered,
                _pending.Count);
        }

        private void Schedule(bool inbound, NetworkPacketHeader header, ArraySegment<byte> payload, Action<NetworkPacketHeader, ArraySegment<byte>> next)
        {
            if (_profile.PacketLossRate > 0d && _random.NextDouble() < _profile.PacketLossRate)
            {
                if (inbound) _inboundDropped++;
                else _outboundDropped++;
                return;
            }

            long now = _clockMs();
            long delay = _profile.BaseLatencyMs;
            if (_profile.JitterMs > 0)
            {
                // Symmetric jitter in [-JitterMs, +JitterMs].
                delay += _random.Next(-_profile.JitterMs, _profile.JitterMs + 1);
            }

            bool reordered = false;
            if (_profile.ReorderRate > 0d && _random.NextDouble() < _profile.ReorderRate)
            {
                // Pull the packet earlier so it can overtake a neighbour scheduled before it.
                long pullForward = _profile.BaseLatencyMs + _profile.JitterMs + 1;
                delay -= pullForward;
                reordered = true;
                if (inbound) _inboundReordered++;
                else _outboundReordered++;
            }

            if (delay < 0) delay = 0;

            // Copy the payload because the caller's buffer may be reused after this call returns.
            var copy = new byte[payload.Count];
            if (payload.Count > 0)
            {
                Buffer.BlockCopy(payload.Array!, payload.Offset, copy, 0, payload.Count);
            }

            _pending.Add(new PendingPacket
            {
                DeliverAtMs = now + delay,
                Sequence = reordered ? long.MinValue + _enqueueCounter++ : _enqueueCounter++,
                Inbound = inbound,
                Header = header,
                Payload = copy,
                Next = next,
            });
        }

        private static long DefaultClock()
        {
            // Environment.TickCount64 is unavailable under Unity's C# profile, so derive a monotonic
            // millisecond clock from the high-resolution timer instead (no 32-bit wraparound).
            return Stopwatch.GetTimestamp() * 1000L / Stopwatch.Frequency;
        }
    }
}
