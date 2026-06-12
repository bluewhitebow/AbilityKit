#nullable enable

using System;
using System.Collections.Generic;

namespace AbilityKit.Network.Runtime
{
    /// <summary>
    /// Gameplay-agnostic ordered buffer of remote authoritative snapshots keyed by a monotonic
    /// timeline value (typically server ticks). Used by <see cref="NetworkSyncModel.AuthoritativeInterpolation"/>
    /// style controllers to retain a short history of remote state so the presentation layer can
    /// play back delayed, smoothly interpolated entity state instead of snapping to the latest push.
    ///
    /// The buffer keeps samples sorted by <see cref="IRemoteSnapshotSample.TimelineTicks"/> ascending,
    /// drops out-of-order or duplicate samples, and trims old samples beyond a bounded capacity.
    /// </summary>
    /// <typeparam name="TSnapshot">The remote snapshot payload type.</typeparam>
    public sealed class RemoteSnapshotBuffer<TSnapshot>
        where TSnapshot : IRemoteSnapshotSample
    {
        public const int DefaultCapacity = 32;

        private readonly List<TSnapshot> _samples;
        private readonly int _capacity;

        public RemoteSnapshotBuffer()
            : this(DefaultCapacity)
        {
        }

        public RemoteSnapshotBuffer(int capacity)
        {
            if (capacity < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be at least 2 to interpolate between two samples.");
            }

            _capacity = capacity;
            _samples = new List<TSnapshot>(capacity);
        }

        public int Count => _samples.Count;

        public int Capacity => _capacity;

        public bool IsEmpty => _samples.Count == 0;

        /// <summary>
        /// The newest buffered timeline value, or <c>null</c> when the buffer is empty.
        /// </summary>
        public long? NewestTimelineTicks => _samples.Count == 0 ? (long?)null : _samples[_samples.Count - 1].TimelineTicks;

        /// <summary>
        /// The oldest buffered timeline value, or <c>null</c> when the buffer is empty.
        /// </summary>
        public long? OldestTimelineTicks => _samples.Count == 0 ? (long?)null : _samples[0].TimelineTicks;

        /// <summary>
        /// Adds a snapshot to the buffer. Snapshots whose timeline value is older than or equal to
        /// the newest buffered sample are rejected as stale, keeping the buffer strictly increasing.
        /// </summary>
        /// <returns><c>true</c> when the snapshot was accepted, <c>false</c> when it was stale/duplicate.</returns>
        public bool TryAdd(TSnapshot snapshot)
        {
            if (_samples.Count > 0 && snapshot.TimelineTicks <= _samples[_samples.Count - 1].TimelineTicks)
            {
                return false;
            }

            _samples.Add(snapshot);
            TrimToCapacity();
            return true;
        }

        /// <summary>
        /// Selects the two samples bracketing the requested timeline value and the interpolation
        /// factor between them. When the target falls before the oldest sample the result clamps to
        /// the oldest sample; when it falls after the newest sample the result reports extrapolation
        /// against the newest sample.
        /// </summary>
        public bool TrySample(long targetTimelineTicks, out RemoteSnapshotInterpolation<TSnapshot> interpolation)
        {
            interpolation = default;
            if (_samples.Count == 0)
            {
                return false;
            }

            if (_samples.Count == 1)
            {
                var only = _samples[0];
                long aheadTicks = targetTimelineTicks - only.TimelineTicks;
                interpolation = new RemoteSnapshotInterpolation<TSnapshot>(only, only, 0f, aheadTicks > 0L ? aheadTicks : 0L);
                return true;
            }

            var oldest = _samples[0];
            if (targetTimelineTicks <= oldest.TimelineTicks)
            {
                interpolation = new RemoteSnapshotInterpolation<TSnapshot>(oldest, oldest, 0f, 0L);
                return true;
            }

            var newest = _samples[_samples.Count - 1];
            if (targetTimelineTicks >= newest.TimelineTicks)
            {
                long extrapolationTicks = targetTimelineTicks - newest.TimelineTicks;
                interpolation = new RemoteSnapshotInterpolation<TSnapshot>(newest, newest, 0f, extrapolationTicks);
                return true;
            }

            for (int i = _samples.Count - 1; i > 0; i--)
            {
                var to = _samples[i];
                var from = _samples[i - 1];
                if (targetTimelineTicks >= from.TimelineTicks && targetTimelineTicks <= to.TimelineTicks)
                {
                    long span = to.TimelineTicks - from.TimelineTicks;
                    float alpha = span <= 0L
                        ? 0f
                        : (float)((targetTimelineTicks - from.TimelineTicks) / (double)span);
                    interpolation = new RemoteSnapshotInterpolation<TSnapshot>(from, to, Clamp01(alpha), 0L);
                    return true;
                }
            }

            // Should be unreachable given the bracketing checks above, but stay safe.
            interpolation = new RemoteSnapshotInterpolation<TSnapshot>(newest, newest, 0f, 0L);
            return true;
        }

        public void Clear()
        {
            _samples.Clear();
        }

        private void TrimToCapacity()
        {
            int overflow = _samples.Count - _capacity;
            if (overflow > 0)
            {
                _samples.RemoveRange(0, overflow);
            }
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }

    /// <summary>
    /// Contract for a remote snapshot stored in a <see cref="RemoteSnapshotBuffer{TSnapshot}"/>.
    /// The timeline value must be monotonically increasing across snapshots; server ticks are the
    /// canonical source in the Shooter sample.
    /// </summary>
    public interface IRemoteSnapshotSample
    {
        long TimelineTicks { get; }
    }

    /// <summary>
    /// The result of sampling a <see cref="RemoteSnapshotBuffer{TSnapshot}"/> at a target timeline
    /// value: the bracketing snapshots, the interpolation factor between them and, when the target is
    /// ahead of the newest sample, how far the playback would have to extrapolate.
    /// </summary>
    public readonly struct RemoteSnapshotInterpolation<TSnapshot>
        where TSnapshot : IRemoteSnapshotSample
    {
        public RemoteSnapshotInterpolation(TSnapshot from, TSnapshot to, float alpha, long extrapolationTicks)
        {
            From = from;
            To = to;
            Alpha = alpha;
            ExtrapolationTicks = extrapolationTicks;
        }

        /// <summary>The earlier bracketing snapshot.</summary>
        public TSnapshot From { get; }

        /// <summary>The later bracketing snapshot. Equals <see cref="From"/> when clamped/extrapolating.</summary>
        public TSnapshot To { get; }

        /// <summary>Interpolation factor in [0,1] between <see cref="From"/> and <see cref="To"/>.</summary>
        public float Alpha { get; }

        /// <summary>
        /// How far past the newest sample the target time fell, in timeline ticks. Zero when the
        /// target was inside the buffered range. Positive values indicate the playback is starved and
        /// holding on the newest sample (an extrapolation policy may choose to act on this).
        /// </summary>
        public long ExtrapolationTicks { get; }

        /// <summary>Whether the sample is a genuine interpolation between two distinct snapshots.</summary>
        public bool IsInterpolating => !ReferenceEquals(From, To) && ExtrapolationTicks == 0L && Alpha > 0f && Alpha < 1f;

        /// <summary>Whether the target time is ahead of the newest buffered snapshot.</summary>
        public bool IsExtrapolating => ExtrapolationTicks > 0L;
    }
}
