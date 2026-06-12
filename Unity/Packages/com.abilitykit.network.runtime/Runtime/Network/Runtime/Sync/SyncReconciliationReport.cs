#nullable enable

namespace AbilityKit.Network.Runtime.Sync
{
    /// <summary>
    /// Gameplay-agnostic reconciliation/catch-up diagnostics produced by a client sync strategy.
    /// Replaces the scattered Last* fields demos previously exposed (e.g. Shooter's LastResyncReason /
    /// LastResyncClientFrame / LastResyncAuthoritativeStateHash ...), giving the demo framework a single
    /// uniform way to surface "why and how the client diverged and recovered" across every sync model.
    ///
    /// Models that never reconcile (e.g. pure authoritative interpolation of remote actors) simply
    /// report <see cref="None"/>.
    /// </summary>
    public readonly struct SyncReconciliationReport
    {
        /// <summary>An empty report indicating no reconciliation has occurred.</summary>
        public static readonly SyncReconciliationReport None = new SyncReconciliationReport(
            SyncReconciliationReason.None,
            SyncRecoveryState.Normal,
            needsFullSnapshot: false,
            clientFrame: 0,
            authoritativeFrame: 0,
            clientStateHash: 0u,
            authoritativeStateHash: 0u,
            replayTicks: 0);

        public SyncReconciliationReport(
            SyncReconciliationReason reason,
            SyncRecoveryState recoveryState,
            bool needsFullSnapshot,
            int clientFrame,
            int authoritativeFrame,
            uint clientStateHash,
            uint authoritativeStateHash,
            int replayTicks)
        {
            Reason = reason;
            RecoveryState = recoveryState;
            NeedsFullSnapshot = needsFullSnapshot;
            ClientFrame = clientFrame;
            AuthoritativeFrame = authoritativeFrame;
            ClientStateHash = clientStateHash;
            AuthoritativeStateHash = authoritativeStateHash;
            ReplayTicks = replayTicks;
        }

        /// <summary>Why the last reconciliation was triggered (or <see cref="SyncReconciliationReason.None"/>).</summary>
        public SyncReconciliationReason Reason { get; }

        /// <summary>The recovery phase the strategy is currently in.</summary>
        public SyncRecoveryState RecoveryState { get; }

        /// <summary>Whether the strategy is awaiting a full authoritative snapshot to recover.</summary>
        public bool NeedsFullSnapshot { get; }

        /// <summary>Local simulation frame at the moment divergence was detected.</summary>
        public int ClientFrame { get; }

        /// <summary>Authoritative server frame involved in the divergence.</summary>
        public int AuthoritativeFrame { get; }

        /// <summary>Locally predicted state hash at the divergence point.</summary>
        public uint ClientStateHash { get; }

        /// <summary>Authoritative state hash at the divergence point.</summary>
        public uint AuthoritativeStateHash { get; }

        /// <summary>Number of ticks replayed during the last rollback/reconciliation (0 when none).</summary>
        public int ReplayTicks { get; }

        /// <summary>True when the last reconciliation actually corrected local state.</summary>
        public bool DidReconcile => Reason != SyncReconciliationReason.None;
    }
}
