#nullable enable

namespace AbilityKit.Network.Runtime.Sync
{
    /// <summary>
    /// Gameplay-agnostic contract for a client-side synchronization model (the "A axis" of the
    /// sync demonstration framework). A strategy encapsulates a single way of keeping the local
    /// client consistent with the authoritative timeline — e.g. predict-and-rollback, or pure
    /// authoritative interpolation of remote actors — independent of any concrete demo (Shooter,
    /// Moba, ...).
    ///
    /// Demos supply two gameplay-specific types and otherwise reuse the strategy verbatim:
    /// <typeparamref name="TInput"/> is the per-tick local command the player produces, and
    /// <typeparamref name="TSample"/> is the decoded remote snapshot sample the strategy plays
    /// back. Everything else (ticking cadence, reconciliation bookkeeping, recovery state) lives
    /// in the framework so a sync model is implemented once and shared across demos.
    /// </summary>
    /// <typeparam name="TInput">The gameplay-specific local input command submitted each tick.</typeparam>
    /// <typeparam name="TSample">The decoded remote snapshot sample observed from the authority.</typeparam>
    public interface IClientSyncStrategy<TInput, TSample> : INetworkSyncController
        where TSample : IRemoteSnapshotSample
    {
        /// <summary>
        /// Advances the strategy by the given frame delta, applying prediction, replay, and/or
        /// interpolation as appropriate for the model, and returns what was simulated this tick.
        /// </summary>
        /// <param name="deltaSeconds">Elapsed time since the previous tick, in seconds.</param>
        SyncTickResult Tick(float deltaSeconds);

        /// <summary>
        /// Records the local player's input for the current tick. Models that do not consume local
        /// input (e.g. pure authoritative interpolation) may ignore it.
        /// </summary>
        /// <param name="input">The local command produced this frame.</param>
        void SubmitInput(in TInput input);

        /// <summary>
        /// Feeds a decoded remote snapshot sample from the authority into the strategy so it can
        /// reconcile, buffer, or interpolate toward authoritative state.
        /// </summary>
        /// <param name="sample">The remote sample observed from the authority.</param>
        void ObserveRemote(in TSample sample);

        /// <summary>
        /// Returns the latest reconciliation/recovery diagnostics, or
        /// <see cref="SyncReconciliationReport.None"/> if the model has not reconciled.
        /// </summary>
        SyncReconciliationReport GetReconciliationReport();
    }
}
