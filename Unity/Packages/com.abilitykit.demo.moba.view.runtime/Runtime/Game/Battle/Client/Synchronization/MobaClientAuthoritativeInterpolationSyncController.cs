#nullable enable

using System;
using AbilityKit.Ability.Host;
using AbilityKit.Network.Runtime;
using AbilityKit.Network.Runtime.Sync;

namespace AbilityKit.Game.Battle.Agent
{
    /// <summary>
    /// Moba client sync controller for <see cref="NetworkSyncModel.AuthoritativeInterpolation"/>, bound to
    /// the gameplay-agnostic framework contract <see cref="IClientSyncStrategy{TInput, TSample}"/> so the
    /// demo framework can drive Moba through the exact same seam as Shooter (Tick / SubmitInput /
    /// ObserveRemote / GetReconciliationReport). This is the second carrier to adopt the A-axis contract,
    /// proving the abstraction is genuinely demo-agnostic: Moba supplies only its own input command
    /// (<see cref="PlayerInputCommand"/>) and remote sample (<see cref="MobaRemoteSnapshotSample"/>); the
    /// buffering, delayed timeline, interpolation and starvation policy all live in the shared framework.
    ///
    /// This controller is a thin wrapper over <see cref="MobaRemoteInterpolationPlayback"/>. It is a pure
    /// addition: existing Moba playback/projection code is untouched, so all existing tests stay green.
    /// </summary>
    public sealed class MobaClientAuthoritativeInterpolationSyncController
        : IClientSyncStrategy<PlayerInputCommand, MobaRemoteSnapshotSample>
    {
        private readonly MobaRemoteInterpolationPlayback _playback;
        private bool _started;
        private int _currentFrame;

        public MobaClientAuthoritativeInterpolationSyncController()
            : this(new MobaRemoteInterpolationPlayback())
        {
        }

        public MobaClientAuthoritativeInterpolationSyncController(InterpolationConfig config)
            : this(new MobaRemoteInterpolationPlayback(config))
        {
        }

        internal MobaClientAuthoritativeInterpolationSyncController(MobaRemoteInterpolationPlayback playback)
        {
            _playback = playback ?? throw new ArgumentNullException(nameof(playback));
        }

        /// <inheritdoc />
        public NetworkSyncModel SyncModel => NetworkSyncModel.AuthoritativeInterpolation;

        /// <inheritdoc />
        public bool IsStarted => _started;

        /// <inheritdoc />
        public int CurrentFrame => _currentFrame;

        /// <summary>Number of remote authoritative snapshots currently buffered for interpolation.</summary>
        public int BufferedRemoteSnapshotCount => _playback.BufferedRemoteSnapshotCount;

        /// <summary>The current local estimate of authoritative server time, in timeline ticks.</summary>
        public long EstimatedServerTicks => _playback.EstimatedServerTicks;

        /// <summary>Whether at least one remote interpolation frame has been projected.</summary>
        public bool HasPublishedRemoteFrame => _playback.HasPublishedRemoteFrame;

        /// <summary>Whether the remote buffer is starved and playback is holding the last authoritative pose.</summary>
        public bool IsRemotePlaybackStarved => _playback.IsRemotePlaybackStarved;

        /// <summary>
        /// Samples the framework playback at the current delayed time and projects the bracketing pair
        /// into an interpolated <see cref="GatewayStateSyncSnapshot"/> for the Moba presentation pipeline.
        /// </summary>
        public bool TryProjectRemoteFrame(out GatewayStateSyncSnapshot snapshot)
            => _playback.TryProjectRemoteFrame(out snapshot);

        /// <summary>Captures the current interpolation playback health for diagnostics / smoke output.</summary>
        public InterpolationDiagnostics GetInterpolationDiagnostics()
            => _playback.GetInterpolationDiagnostics();

        /// <summary>Clears the buffer and resets the timeline, playback flags and controller state.</summary>
        public void Reset()
        {
            _playback.Reset();
            _started = false;
            _currentFrame = 0;
        }

        /// <inheritdoc />
        public SyncTickResult Tick(float deltaSeconds)
        {
            _started = true;
            _playback.Advance(deltaSeconds);

            if (_playback.TryProjectRemoteFrame(out var snapshot))
            {
                _currentFrame = snapshot.Frame;
                return new SyncTickResult(ticks: 0, frame: snapshot.Frame, stateHash: 0u);
            }

            return new SyncTickResult(ticks: 0, frame: _currentFrame, stateHash: 0u);
        }

        /// <inheritdoc />
        public void SubmitInput(in PlayerInputCommand input)
        {
            // No-op for authoritative interpolation: remote actors are driven purely by observed
            // authoritative samples, never by locally predicted input. The local player's command flows
            // through the gameplay command pipeline (gateway submission), not this presentation strategy.
        }

        /// <inheritdoc />
        public void ObserveRemote(in MobaRemoteSnapshotSample sample)
            => _playback.Observe(in sample);

        /// <inheritdoc />
        public SyncReconciliationReport GetReconciliationReport() => SyncReconciliationReport.None;
    }
}
