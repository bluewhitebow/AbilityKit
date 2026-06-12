using System;

namespace AbilityKit.Network.Runtime.Sync
{
    public enum ClientPlaybackPolicy
    {
        None = 0,
        PredictRollback = 1,
        AuthoritativeInterpolation = 2,
        HoldLatest = 3,
        ExtrapolateThenCorrect = 4,
        HybridLocalPredictRemoteInterpolate = 5
    }

    [Flags]
    public enum InputPolicy
    {
        None = 0,
        NoClientInput = 1 << 0,
        ImmediateSubmit = 1 << 1,
        InputDelayBuffer = 1 << 2,
        ServerRemapAcceptedFrame = 1 << 3,
        DeterministicBroadcast = 1 << 4
    }

    [Flags]
    public enum SnapshotPolicy
    {
        None = 0,
        FullSnapshot = 1 << 0,
        DeltaSnapshot = 1 << 1,
        KeyFrameSnapshot = 1 << 2,
        AuthorityOverride = 1 << 3,
        FixedRateStateStream = 1 << 4,
        BatchSnapshot = 1 << 5,
        EventStream = 1 << 6
    }

    [Flags]
    public enum InterestPolicy
    {
        None = 0,
        AllEntities = 1 << 0,
        OwnerRelevant = 1 << 1,
        DistanceAoi = 1 << 2,
        TeamOrFactionAoi = 1 << 3,
        PriorityBudget = 1 << 4,
        LodFrequency = 1 << 5
    }

    [Flags]
    public enum RecoveryPolicy
    {
        None = 0,
        RequestFullSnapshot = 1 << 0,
        RequestKeyFrame = 1 << 1,
        RequestAoiSlice = 1 << 2,
        CatchUpToServerFrame = 1 << 3,
        ReconnectResume = 1 << 4
    }

    [Flags]
    public enum ServerValidationPolicy
    {
        None = 0,
        AuthoritativeOnly = 1 << 0,
        InputValidation = 1 << 1,
        LagCompensatedHitValidation = 1 << 2,
        ClientHashAudit = 1 << 3,
        AntiCheatEnvelope = 1 << 4
    }

    public readonly struct NetworkSyncProfile : IEquatable<NetworkSyncProfile>
    {
        public NetworkSyncProfile(
            NetworkSyncModel compatibilityModel,
            ClientPlaybackPolicy clientPlayback,
            InputPolicy input,
            SnapshotPolicy snapshot,
            InterestPolicy interest,
            RecoveryPolicy recovery,
            ServerValidationPolicy serverValidation)
        {
            CompatibilityModel = compatibilityModel;
            ClientPlayback = clientPlayback;
            Input = input;
            Snapshot = snapshot;
            Interest = interest;
            Recovery = recovery;
            ServerValidation = serverValidation;
        }

        public NetworkSyncModel CompatibilityModel { get; }

        public ClientPlaybackPolicy ClientPlayback { get; }

        public InputPolicy Input { get; }

        public SnapshotPolicy Snapshot { get; }

        public InterestPolicy Interest { get; }

        public RecoveryPolicy Recovery { get; }

        public ServerValidationPolicy ServerValidation { get; }

        public bool Equals(NetworkSyncProfile other)
        {
            return CompatibilityModel == other.CompatibilityModel &&
                   ClientPlayback == other.ClientPlayback &&
                   Input == other.Input &&
                   Snapshot == other.Snapshot &&
                   Interest == other.Interest &&
                   Recovery == other.Recovery &&
                   ServerValidation == other.ServerValidation;
        }

        public override bool Equals(object? obj)
        {
            return obj is NetworkSyncProfile other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                CompatibilityModel,
                ClientPlayback,
                Input,
                Snapshot,
                Interest,
                Recovery,
                ServerValidation);
        }

        public static bool operator ==(NetworkSyncProfile left, NetworkSyncProfile right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(NetworkSyncProfile left, NetworkSyncProfile right)
        {
            return !left.Equals(right);
        }
    }
}
