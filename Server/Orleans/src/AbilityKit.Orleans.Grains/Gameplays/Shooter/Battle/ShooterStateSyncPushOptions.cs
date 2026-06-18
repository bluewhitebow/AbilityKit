using AbilityKit.Network.Runtime.Conditioning;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Orleans.Grains.Gameplays.Shooter.Battle;

internal enum ShooterStateSyncPushPayloadMode
{
    Packed = 0,
    PureState = 1
}

internal sealed class ShooterStateSyncPushOptions
{
    private const int LimitedBandwidthKbps = 256;
    private const int HighLatencyMs = 120;
    private const double LossyLinkRate = 0.02d;

    private ShooterStateSyncPushOptions(
        ShooterStateSyncPushPayloadMode payloadMode,
        NetworkConditionProfile networkCondition,
        ShooterPureStateSyncSettings? pureStateSettings)
    {
        PayloadMode = payloadMode;
        NetworkCondition = networkCondition;
        PureStateSettings = pureStateSettings;
    }

    public ShooterStateSyncPushPayloadMode PayloadMode { get; }

    public NetworkConditionProfile NetworkCondition { get; }

    public ShooterPureStateSyncSettings? PureStateSettings { get; }

    public static ShooterStateSyncPushOptions PackedDefault { get; } = new ShooterStateSyncPushOptions(
        ShooterStateSyncPushPayloadMode.Packed,
        NetworkConditionProfile.Ideal,
        null);

    public static ShooterStateSyncPushOptions PureState(NetworkConditionProfile networkCondition, ShooterPureStateSyncSettings? settings = null)
    {
        return new ShooterStateSyncPushOptions(ShooterStateSyncPushPayloadMode.PureState, networkCondition, settings);
    }

    public ShooterPureStateSyncSettings ResolvePureStateSettings()
    {
        if (PureStateSettings.HasValue)
        {
            return PureStateSettings.Value;
        }

        var defaults = ShooterPureStateSyncSettings.Default;
        if (NetworkCondition.BandwidthKbps > 0 && NetworkCondition.BandwidthKbps <= LimitedBandwidthKbps)
        {
            return new ShooterPureStateSyncSettings(
                defaults.MaxEntityCount,
                128,
                defaults.BaselineIntervalFrames,
                4,
                30,
                6);
        }

        if (NetworkCondition.PacketLossRate >= LossyLinkRate || NetworkCondition.JitterMs >= 50)
        {
            return new ShooterPureStateSyncSettings(
                defaults.MaxEntityCount,
                256,
                defaults.BaselineIntervalFrames,
                3,
                24,
                6);
        }

        if (NetworkCondition.BaseLatencyMs >= HighLatencyMs)
        {
            return new ShooterPureStateSyncSettings(
                defaults.MaxEntityCount,
                384,
                defaults.BaselineIntervalFrames,
                3,
                20,
                5);
        }

        return defaults;
    }
}
