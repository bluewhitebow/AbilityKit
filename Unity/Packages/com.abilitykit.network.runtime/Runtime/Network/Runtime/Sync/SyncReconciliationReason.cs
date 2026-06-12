#nullable enable

namespace AbilityKit.Network.Runtime.Sync
{
    /// <summary>
    /// Gameplay-agnostic reason a client sync strategy decided its local state diverged from the
    /// authoritative server state and required correction. This unifies what individual demos used
    /// to express through their own bespoke enums (e.g. Shooter's ShooterClientResyncReason), so the
    /// demo framework can report reconciliation uniformly across every <see cref="NetworkSyncModel"/>.
    /// </summary>
    public enum SyncReconciliationReason
    {
        /// <summary>No reconciliation was needed; local state tracked the server.</summary>
        None = 0,

        /// <summary>Failed to import/apply an authoritative snapshot.</summary>
        ImportFailed = 1,

        /// <summary>Authoritative state hash disagreed with the locally predicted hash.</summary>
        AuthoritativeHashMismatch = 2,

        /// <summary>Server rejected a client-reported state hash.</summary>
        ClientHashRejected = 3,

        /// <summary>Local simulation fell too far behind the authoritative frame to catch up incrementally.</summary>
        FrameTooFarBehind = 4,

        /// <summary>Local simulation ran too far ahead of the authoritative frame.</summary>
        FrameTooFarAhead = 5,

        /// <summary>An expected authoritative snapshot did not arrive within the allowed window.</summary>
        SnapshotTimeout = 6,

        /// <summary>The authoritative update targeted a different world/session than the local one.</summary>
        WorldMismatch = 7
    }
}
