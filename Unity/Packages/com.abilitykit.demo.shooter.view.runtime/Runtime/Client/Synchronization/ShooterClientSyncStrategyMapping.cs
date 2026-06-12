#nullable enable

using AbilityKit.Network.Runtime.Sync;

namespace AbilityKit.Demo.Shooter.View
{
    /// <summary>
    /// Maps Shooter's demo-specific sync diagnostics onto the gameplay-agnostic framework
    /// contracts (<see cref="SyncTickResult"/> / <see cref="SyncReconciliationReport"/>) so the
    /// Shooter controllers can satisfy <see cref="IClientSyncStrategy{TInput, TSample}"/> without
    /// changing their existing demo-facing surface.
    ///
    /// This is the thin adapter seam from migration step 3: the demo keeps its rich
    /// <see cref="ShooterClientFrameTickResult"/> / <see cref="ShooterClientReconciliationResult"/>
    /// types, and these projections expose the subset the framework needs.
    /// </summary>
    internal static class ShooterClientSyncStrategyMapping
    {
        public static SyncTickResult ToSyncTickResult(in ShooterClientFrameTickResult tick)
        {
            return new SyncTickResult(tick.Ticks, tick.Frame, tick.StateHash);
        }

        public static SyncReconciliationReason ToReason(ShooterClientResyncReason reason)
        {
            // The framework enum was modelled directly on Shooter's reasons, so the numeric values
            // line up one-to-one; map explicitly to stay robust against future divergence.
            switch (reason)
            {
                case ShooterClientResyncReason.ImportFailed: return SyncReconciliationReason.ImportFailed;
                case ShooterClientResyncReason.AuthoritativeHashMismatch: return SyncReconciliationReason.AuthoritativeHashMismatch;
                case ShooterClientResyncReason.ClientHashRejectedByServer: return SyncReconciliationReason.ClientHashRejected;
                case ShooterClientResyncReason.FrameTooFarBehind: return SyncReconciliationReason.FrameTooFarBehind;
                case ShooterClientResyncReason.FrameTooFarAhead: return SyncReconciliationReason.FrameTooFarAhead;
                case ShooterClientResyncReason.SnapshotTimeout: return SyncReconciliationReason.SnapshotTimeout;
                case ShooterClientResyncReason.WorldMismatch: return SyncReconciliationReason.WorldMismatch;
                case ShooterClientResyncReason.None:
                default:
                    return SyncReconciliationReason.None;
            }
        }

        public static SyncRecoveryState ToRecoveryState(ShooterClientRecoveryState state)
        {
            switch (state)
            {
                case ShooterClientRecoveryState.CatchUp: return SyncRecoveryState.CatchUp;
                case ShooterClientRecoveryState.AwaitingFullSnapshot: return SyncRecoveryState.AwaitingFullSnapshot;
                case ShooterClientRecoveryState.ApplyingFullSnapshot: return SyncRecoveryState.ApplyingFullSnapshot;
                case ShooterClientRecoveryState.Recovered: return SyncRecoveryState.Recovered;
                case ShooterClientRecoveryState.Normal:
                default:
                    return SyncRecoveryState.Normal;
            }
        }

        /// <summary>
        /// Builds a framework reconciliation report from a controller's current diagnostic state.
        /// Uses the controller's last resync metadata for the divergence frames/hashes and its last
        /// reconciliation result for the replay tick count.
        /// </summary>
        public static SyncReconciliationReport ToReconciliationReport(IShooterClientSyncController controller)
        {
            return new SyncReconciliationReport(
                ToReason(controller.LastResyncReason),
                ToRecoveryState(controller.RecoveryState),
                controller.NeedsFullSnapshotResync,
                controller.LastResyncClientFrame,
                controller.LastResyncAuthoritativeFrame,
                controller.LastResyncClientStateHash,
                controller.LastResyncAuthoritativeStateHash,
                controller.LastReconciliationResult.ReplayTicks);
        }
    }
}
