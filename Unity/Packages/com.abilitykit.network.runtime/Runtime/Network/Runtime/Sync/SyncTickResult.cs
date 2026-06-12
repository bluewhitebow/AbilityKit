#nullable enable

namespace AbilityKit.Network.Runtime.Sync
{
    /// <summary>
    /// Gameplay-agnostic result of advancing a client sync strategy by one update.
    /// Captures how far local simulation/playback progressed, which the demo framework can use to
    /// drive presentation and to compare behaviour across sync models and network profiles.
    /// </summary>
    public readonly struct SyncTickResult
    {
        /// <summary>A result indicating the strategy did not advance (e.g. not started).</summary>
        public static readonly SyncTickResult NotStarted = new SyncTickResult(ticks: 0, frame: 0, stateHash: 0u);

        public SyncTickResult(int ticks, int frame, uint stateHash)
        {
            Ticks = ticks;
            Frame = frame;
            StateHash = stateHash;
        }

        /// <summary>Number of fixed simulation/playback ticks advanced during this update.</summary>
        public int Ticks { get; }

        /// <summary>The frame the strategy advanced its local simulation or playback to.</summary>
        public int Frame { get; }

        /// <summary>State hash of the local simulation after this update (0 when not applicable).</summary>
        public uint StateHash { get; }
    }
}
