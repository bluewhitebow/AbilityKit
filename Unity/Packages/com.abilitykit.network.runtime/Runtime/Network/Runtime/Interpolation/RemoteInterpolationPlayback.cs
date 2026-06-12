#nullable enable

using System;

namespace AbilityKit.Network.Runtime
{
    /// <summary>
    /// Gameplay-agnostic orchestration for <see cref="NetworkSyncModel.AuthoritativeInterpolation"/>
    /// style remote playback. It owns the <see cref="RemoteSnapshotBuffer{TSnapshot}"/> +
    /// <see cref="InterpolationTimeline"/> pair and the extrapolation/starvation policy, so each demo
    /// only has to: (1) decode an incoming push into a <typeparamref name="TSample"/> and feed it via
    /// <see cref="Observe"/>, and (2) project + apply the sampled interpolation to its presentation.
    ///
    /// Typical per-frame usage:
    /// <code>
    /// playback.Advance(deltaSeconds);
    /// if (playback.TrySample(out var interpolation))
    /// {
    ///     var projected = project(in interpolation);
    ///     presentation.Apply(in projected);
    /// }
    /// </code>
    /// </summary>
    /// <typeparam name="TSample">The buffered remote snapshot sample type.</typeparam>
    public sealed class RemoteInterpolationPlayback<TSample>
        where TSample : IRemoteSnapshotSample
    {
        private readonly RemoteSnapshotBuffer<TSample> _buffer;
        private readonly InterpolationTimeline _timeline;
        private readonly long _maxExtrapolationTicks;

        public RemoteInterpolationPlayback()
            : this(InterpolationConfig.Default)
        {
        }

        public RemoteInterpolationPlayback(InterpolationConfig config)
        {
            _buffer = new RemoteSnapshotBuffer<TSample>(config.BufferCapacity);
            _timeline = new InterpolationTimeline(config.TicksPerSecond, config.InterpolationDelayTicks, config.CatchUpRate);
            _maxExtrapolationTicks = config.MaxExtrapolationTicks;
        }

        /// <summary>Number of remote authoritative snapshots currently buffered for interpolation.</summary>
        public int BufferedSampleCount => _buffer.Count;

        /// <summary>The current delayed remote playback time, in timeline ticks.</summary>
        public long PlaybackTicks => _timeline.PlaybackTicks;

        /// <summary>The current local estimate of authoritative server time, in timeline ticks.</summary>
        public long EstimatedServerTicks => _timeline.EstimatedServerTicks;

        /// <summary>Whether at least one remote interpolation sample has been produced via <see cref="TrySample"/>.</summary>
        public bool HasPublished { get; private set; }

        /// <summary>
        /// Whether the most recent <see cref="TrySample"/> found the delayed playback time running past
        /// the newest buffered snapshot by more than <see cref="InterpolationConfig.MaxExtrapolationTicks"/>.
        /// Indicates the buffer is starved (e.g. snapshots stopped arriving) and playback is holding the
        /// last authoritative pose rather than extrapolating further.
        /// </summary>
        public bool IsStarved { get; private set; }

        /// <summary>
        /// Buffers a decoded remote authoritative sample and folds its server time into the timeline.
        /// Stale/duplicate samples (timeline value not strictly ahead of the newest buffered sample) are
        /// rejected and do not advance the timeline.
        /// </summary>
        /// <returns><c>true</c> when the sample was accepted, <c>false</c> when it was stale/duplicate.</returns>
        public bool Observe(TSample sample)
        {
            if (!_buffer.TryAdd(sample))
            {
                return false;
            }

            _timeline.ObserveServerTicks(sample.TimelineTicks);
            return true;
        }

        /// <summary>Advances the delayed playback timeline by a frame delta.</summary>
        public void Advance(float deltaSeconds)
        {
            _timeline.Advance(deltaSeconds);
        }

        /// <summary>
        /// Samples the buffer at the current delayed playback time. Returns <c>false</c> until at least
        /// one authoritative time has been observed and the buffer is non-empty. On success it updates
        /// <see cref="IsStarved"/> (per the extrapolation policy) and marks <see cref="HasPublished"/>.
        ///
        /// Extrapolation policy: when the delayed playback time runs past the newest buffered snapshot
        /// the buffer is starved. Remote poses are deliberately not extrapolated forward (that would
        /// invent unauthoritative motion); the returned interpolation holds the newest sample. Once the
        /// gap exceeds the configured tolerance the playback is additionally flagged as starved so
        /// callers can react (e.g. surface a connection-quality hint).
        /// </summary>
        public bool TrySample(out RemoteSnapshotInterpolation<TSample> interpolation)
        {
            interpolation = default;
            if (!_timeline.HasServerTime || _buffer.IsEmpty)
            {
                return false;
            }

            if (!_buffer.TrySample(_timeline.PlaybackTicks, out interpolation))
            {
                return false;
            }

            IsStarved = interpolation.ExtrapolationTicks > _maxExtrapolationTicks;
            HasPublished = true;
            return true;
        }

        /// <summary>Captures the current interpolation playback health for diagnostics / smoke output.</summary>
        public InterpolationDiagnostics GetDiagnostics()
        {
            return new InterpolationDiagnostics(
                bufferedRemoteSnapshotCount: BufferedSampleCount,
                remotePlaybackTicks: PlaybackTicks,
                estimatedServerTicks: EstimatedServerTicks,
                hasPublishedRemoteFrame: HasPublished,
                isRemotePlaybackStarved: IsStarved);
        }

        /// <summary>Clears the buffer and resets the timeline and playback flags.</summary>
        public void Reset()
        {
            _buffer.Clear();
            _timeline.Reset();
            HasPublished = false;
            IsStarved = false;
        }
    }
}
