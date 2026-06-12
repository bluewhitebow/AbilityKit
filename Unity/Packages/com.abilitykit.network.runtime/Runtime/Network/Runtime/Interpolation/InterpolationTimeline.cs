#nullable enable

using System;

namespace AbilityKit.Network.Runtime
{
    /// <summary>
    /// Gameplay-agnostic interpolation timeline for <see cref="NetworkSyncModel.AuthoritativeInterpolation"/>
    /// style playback. It advances a local estimate of server time from frame delta time, folds in
    /// authoritative server ticks observed on incoming snapshots, and exposes the delayed playback
    /// time used to sample a <see cref="RemoteSnapshotBuffer{TSnapshot}"/>.
    ///
    /// The timeline intentionally keeps remote playback a fixed <see cref="InterpolationDelayTicks"/>
    /// behind the newest authoritative sample so that, under normal jitter, the target time falls
    /// between two buffered snapshots and remote entities move smoothly instead of snapping.
    ///
    /// Two correction behaviours are supported. With the default (<see cref="MaxCatchUpRate"/> == 0)
    /// the local estimate snaps directly to the authoritative target, which is simple and fully
    /// deterministic. With a positive catch-up rate the estimate instead converges toward the
    /// authoritative target over several frames at a bounded rate, so clock drift between the local
    /// frame clock and the server tick stream is absorbed smoothly rather than via a visible jump.
    /// </summary>
    public sealed class InterpolationTimeline
    {
        // Authoritative target time: advanced by frame delta and corrected (forward only) by observed
        // server ticks. The estimate trails this in soft catch-up mode and equals it in snap mode.
        private double _targetTicks;
        private double _estimatedTicks;
        private bool _hasServerTime;
        private readonly double _maxCatchUpRate;

        public InterpolationTimeline(long ticksPerSecond, long interpolationDelayTicks)
            : this(ticksPerSecond, interpolationDelayTicks, 0d)
        {
        }

        /// <param name="ticksPerSecond">Timeline resolution: how many ticks represent one second.</param>
        /// <param name="interpolationDelayTicks">How far behind the newest authoritative time playback is held.</param>
        /// <param name="maxCatchUpRate">
        /// The maximum extra fraction of a frame's advance that may be spent closing the gap between the
        /// estimate and the authoritative target each frame. <c>0</c> snaps the estimate to the target;
        /// e.g. <c>0.1</c> lets the estimate catch up at up to 10% faster (or slower) than real time so
        /// drift is absorbed smoothly. Values are clamped to <c>[0, 1]</c>.
        /// </param>
        public InterpolationTimeline(long ticksPerSecond, long interpolationDelayTicks, double maxCatchUpRate)
        {
            if (ticksPerSecond <= 0L)
            {
                ticksPerSecond = 1L;
            }

            TicksPerSecond = ticksPerSecond;
            InterpolationDelayTicks = interpolationDelayTicks < 0L ? 0L : interpolationDelayTicks;
            _maxCatchUpRate = maxCatchUpRate < 0d ? 0d : (maxCatchUpRate > 1d ? 1d : maxCatchUpRate);
        }

        /// <summary>Timeline resolution: how many ticks represent one second of server time.</summary>
        public long TicksPerSecond { get; }

        /// <summary>How far behind the newest authoritative time playback is held, in ticks.</summary>
        public long InterpolationDelayTicks { get; }

        /// <summary>
        /// The bounded soft catch-up rate. Zero means the estimate snaps to the authoritative target;
        /// a positive value means the estimate converges smoothly toward it.
        /// </summary>
        public double MaxCatchUpRate => _maxCatchUpRate;

        /// <summary>Whether the timeline has observed at least one authoritative server time.</summary>
        public bool HasServerTime => _hasServerTime;

        /// <summary>The current local estimate of server time, in ticks.</summary>
        public long EstimatedServerTicks => (long)_estimatedTicks;

        /// <summary>
        /// The authoritative target server time the estimate is converging toward, in ticks. In snap
        /// mode this equals <see cref="EstimatedServerTicks"/>.
        /// </summary>
        public long TargetServerTicks => (long)_targetTicks;

        /// <summary>
        /// The delayed playback time used to sample the remote snapshot buffer. This is the estimated
        /// server time minus the interpolation delay, never negative.
        /// </summary>
        public long PlaybackTicks
        {
            get
            {
                long playback = EstimatedServerTicks - InterpolationDelayTicks;
                return playback < 0L ? 0L : playback;
            }
        }

        /// <summary>
        /// Folds an authoritative server time (from a received snapshot) into the timeline. The first
        /// observation seeds both the target and the estimate. Later observations move the target
        /// forward only (stale times are ignored). In snap mode the estimate is pulled forward with the
        /// target; in soft catch-up mode the estimate is left to converge during <see cref="Advance"/>.
        /// </summary>
        public void ObserveServerTicks(long serverTicks)
        {
            if (!_hasServerTime)
            {
                _targetTicks = serverTicks;
                _estimatedTicks = serverTicks;
                _hasServerTime = true;
                return;
            }

            if (serverTicks > _targetTicks)
            {
                _targetTicks = serverTicks;
            }

            if (_maxCatchUpRate <= 0d)
            {
                _estimatedTicks = _targetTicks;
            }
        }

        /// <summary>
        /// Advances the local server time estimate by a frame delta. No-op until the first
        /// authoritative server time has been observed. The authoritative target advances at real time;
        /// in soft catch-up mode the estimate advances at real time plus a bounded correction toward the
        /// target so accumulated drift is absorbed gradually.
        /// </summary>
        public void Advance(float deltaSeconds)
        {
            if (!_hasServerTime || deltaSeconds <= 0f)
            {
                return;
            }

            double advance = deltaSeconds * TicksPerSecond;
            _targetTicks += advance;

            if (_maxCatchUpRate <= 0d)
            {
                _estimatedTicks = _targetTicks;
                return;
            }

            double error = _targetTicks - _estimatedTicks;
            double maxCorrection = advance * _maxCatchUpRate;
            double correction = error * _maxCatchUpRate;
            if (correction > maxCorrection)
            {
                correction = maxCorrection;
            }
            else if (correction < -maxCorrection)
            {
                correction = -maxCorrection;
            }

            _estimatedTicks += advance + correction;
        }

        public void Reset()
        {
            _targetTicks = 0d;
            _estimatedTicks = 0d;
            _hasServerTime = false;
        }
    }
}
