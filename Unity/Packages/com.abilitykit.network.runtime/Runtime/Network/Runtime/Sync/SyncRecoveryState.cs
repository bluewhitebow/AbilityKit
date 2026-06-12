#nullable enable

namespace AbilityKit.Network.Runtime.Sync
{
    /// <summary>
    /// Gameplay-agnostic recovery phase a client sync strategy is currently in. Unifies what demos
    /// previously expressed through bespoke enums (e.g. Shooter's ShooterClientRecoveryState).
    /// </summary>
    public enum SyncRecoveryState
    {
        /// <summary>Local simulation/playback is tracking the server normally.</summary>
        Normal = 0,

        /// <summary>Catching up a small frame deficit by fast-forwarding local simulation.</summary>
        CatchUp = 1,

        /// <summary>Drift exceeded incremental recovery; waiting for a full authoritative snapshot.</summary>
        AwaitingFullSnapshot = 2,

        /// <summary>Applying a received full authoritative snapshot.</summary>
        ApplyingFullSnapshot = 3,

        /// <summary>Recovery just completed and local state was restored to authority.</summary>
        Recovered = 4
    }
}
