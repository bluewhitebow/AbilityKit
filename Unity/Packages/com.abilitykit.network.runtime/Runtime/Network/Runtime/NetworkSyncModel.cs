namespace AbilityKit.Network.Runtime
{
    /// <summary>
    /// Describes the network synchronization strategy used by a session or gameplay sample.
    /// This is intentionally gameplay-agnostic so samples can share the same sync vocabulary.
    /// </summary>
    public enum NetworkSyncModel
    {
        /// <summary>
        /// No explicit network synchronization model has been selected.
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// Deterministic input synchronization without client rollback recovery.
        /// Not implemented yet — reserved for a future lockstep sync strategy.
        /// </summary>
        Lockstep = 1,

        /// <summary>
        /// Local prediction, authoritative snapshots, rollback, replay and reconciliation.
        /// Implemented (see the predict-rollback client sync strategy).
        /// </summary>
        PredictRollback = 2,

        /// <summary>
        /// Server authoritative entity state snapshots consumed through interpolation or extrapolation.
        /// Implemented (see the authoritative-interpolation client sync strategy).
        /// </summary>
        AuthoritativeInterpolation = 3,

        /// <summary>
        /// Server authoritative state updates published in low-frequency batches.
        /// Not implemented yet — reserved for a future batched state-sync strategy.
        /// </summary>
        BatchStateSync = 4,

        /// <summary>
        /// Large-scale server authoritative synchronization with interest management and LOD policies.
        /// Not implemented yet — reserved for a future mass-battle LOD sync strategy.
        /// </summary>
        MassBattleLodSync = 5,

        /// <summary>
        /// Mixed strategy, usually local hero prediction plus remote interpolation and batched ordinary units.
        /// Not implemented yet — reserved for a future hybrid hero-prediction strategy.
        /// </summary>
        HybridHeroPrediction = 6,

        /// <summary>
        /// Fast reconnect and state restoration flow built around targeted snapshots.
        /// Not implemented yet — reserved for a future fast-reconnect strategy.
        /// </summary>
        FastReconnect = 7,

        /// <summary>
        /// Server-side rewind of authoritative history for latency-compensated hit validation.
        /// Implemented by the server rewind lag compensation helper.
        /// </summary>
        ServerRewindLagCompensation = 8
    }
}
