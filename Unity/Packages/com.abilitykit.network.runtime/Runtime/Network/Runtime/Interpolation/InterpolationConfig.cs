#nullable enable

namespace AbilityKit.Network.Runtime
{
    /// <summary>
    /// Gameplay-agnostic tuning for authoritative remote playback driven by
    /// <see cref="RemoteSnapshotBuffer{TSample}"/> + <see cref="InterpolationTimeline"/>.
    /// <see cref="TicksPerSecond"/> maps the authoritative server-tick timeline to wall-clock seconds,
    /// and <see cref="InterpolationDelayTicks"/> is how far behind the newest authoritative sample the
    /// remote playback is held so jitter is absorbed and interpolation has two samples to blend.
    /// <see cref="BufferCapacity"/> bounds how many remote snapshots are retained.
    /// </summary>
    public readonly struct InterpolationConfig
    {
        public InterpolationConfig(long ticksPerSecond, long interpolationDelayTicks, int bufferCapacity)
            : this(ticksPerSecond, interpolationDelayTicks, bufferCapacity, DefaultCatchUpRate, DefaultMaxExtrapolationTicks)
        {
        }

        public InterpolationConfig(long ticksPerSecond, long interpolationDelayTicks, int bufferCapacity, double catchUpRate)
            : this(ticksPerSecond, interpolationDelayTicks, bufferCapacity, catchUpRate, DefaultMaxExtrapolationTicks)
        {
        }

        public InterpolationConfig(long ticksPerSecond, long interpolationDelayTicks, int bufferCapacity, double catchUpRate, long maxExtrapolationTicks)
        {
            TicksPerSecond = ticksPerSecond <= 0L ? 1L : ticksPerSecond;
            InterpolationDelayTicks = interpolationDelayTicks < 0L ? 0L : interpolationDelayTicks;
            BufferCapacity = bufferCapacity < 2 ? 2 : bufferCapacity;
            CatchUpRate = catchUpRate < 0d ? 0d : (catchUpRate > 1d ? 1d : catchUpRate);
            MaxExtrapolationTicks = maxExtrapolationTicks < 0L ? 0L : maxExtrapolationTicks;
        }

        /// <summary>Default soft catch-up rate: absorb clock drift at up to 10% of real time per frame.</summary>
        public const double DefaultCatchUpRate = 0.1d;

        /// <summary>
        /// Default extrapolation tolerance: when the delayed playback time runs more than 50ms past the
        /// newest buffered snapshot (a starved buffer), playback holds the last authoritative pose and
        /// is flagged as starved rather than drifting further.
        /// </summary>
        public const long DefaultMaxExtrapolationTicks = 50L;

        public long TicksPerSecond { get; }

        public long InterpolationDelayTicks { get; }

        public int BufferCapacity { get; }

        /// <summary>
        /// How aggressively the playback clock converges toward the authoritative server time. Zero
        /// snaps directly; a positive value (clamped to 1) smoothly absorbs drift. See
        /// <see cref="InterpolationTimeline.MaxCatchUpRate"/>.
        /// </summary>
        public double CatchUpRate { get; }

        /// <summary>
        /// How far past the newest buffered snapshot the delayed playback may run before it is treated
        /// as starved. Within this tolerance playback holds on the newest authoritative pose; beyond it
        /// the controller flags the buffer as starved (see
        /// <see cref="InterpolationDiagnostics.IsRemotePlaybackStarved"/>).
        /// </summary>
        public long MaxExtrapolationTicks { get; }

        /// <summary>
        /// Default tuning: a 100ms interpolation delay against a millisecond timeline, retaining the
        /// last 32 remote snapshots, with soft clock convergence and a 50ms extrapolation tolerance.
        /// Samples that carry server ticks in other units can supply a matching
        /// <see cref="TicksPerSecond"/>.
        /// </summary>
        public static InterpolationConfig Default => new InterpolationConfig(
            ticksPerSecond: 1000L,
            interpolationDelayTicks: 100L,
            bufferCapacity: 32,
            catchUpRate: DefaultCatchUpRate,
            maxExtrapolationTicks: DefaultMaxExtrapolationTicks);
    }
}
