#nullable enable

using System;
using AbilityKit.Ability.Host;
using AbilityKit.Network.Runtime;
using AbilityKit.Network.Runtime.Conditioning;
using AbilityKit.Network.Runtime.DemoHarness;
using AbilityKit.Network.Runtime.Sync;

namespace AbilityKit.Game.Battle.Agent
{
    /// <summary>
    /// Thin adapter that lets the framework demo harness drive an existing Moba sync strategy.
    /// It does not own Moba lifecycle; callers configure the underlying controller before running scenarios.
    /// </summary>
    public sealed class MobaDemoHarnessCarrier : ISyncDemoCarrier, ISyncDemoCarrierCapabilities
    {
        public const string DefaultCarrierName = "Moba";

        private readonly IClientSyncStrategy<PlayerInputCommand, MobaRemoteSnapshotSample> _strategy;
        private readonly Func<NetworkConditioningStats> _networkStats;
        private readonly Func<double> _remoteJitter;
        private readonly Func<long> _acceptedHits;
        private readonly Func<long> _rejectedHits;

        public MobaDemoHarnessCarrier(
            IClientSyncStrategy<PlayerInputCommand, MobaRemoteSnapshotSample> strategy,
            Func<NetworkConditioningStats>? networkStats = null,
            Func<double>? remoteJitter = null,
            Func<long>? acceptedHits = null,
            Func<long>? rejectedHits = null,
            string carrierName = DefaultCarrierName)
        {
            if (string.IsNullOrWhiteSpace(carrierName)) throw new ArgumentException("Carrier name is required.", nameof(carrierName));

            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _networkStats = networkStats ?? (() => default);
            _remoteJitter = remoteJitter ?? (() => 0d);
            _acceptedHits = acceptedHits ?? (() => 0L);
            _rejectedHits = rejectedHits ?? (() => 0L);
            CarrierName = carrierName;
        }

        public string CarrierName { get; }

        public NetworkSyncModel SyncModel => _strategy.SyncModel;

        public SyncTimeAnchor LastTimeAnchor { get; private set; }

        public SyncDemoCapabilityResult Supports(in NetworkSyncProfile profile, in NetworkConditionProfile networkProfile)
        {
            if (profile.CompatibilityModel == NetworkSyncModel.MassBattleLodSync)
            {
                return SyncDemoCapabilityResult.Degraded("Moba carrier does not yet implement AOI or LOD budgets; running as all-entities authoritative interpolation.");
            }

            if (profile.ClientPlayback != ClientPlaybackPolicy.AuthoritativeInterpolation)
            {
                return SyncDemoCapabilityResult.Unsupported("Moba carrier currently supports authoritative interpolation playback only.");
            }

            if (!profile.Snapshot.HasFlag(SnapshotPolicy.FixedRateStateStream) && !profile.Snapshot.HasFlag(SnapshotPolicy.BatchSnapshot))
            {
                return SyncDemoCapabilityResult.Unsupported("Moba carrier requires a fixed-rate or batch state stream.");
            }

            return SyncDemoCapabilityResult.Supported;
        }

        public DemoHarnessStepTelemetry Step(in DemoHarnessStepContext context)
        {
            LastTimeAnchor = context.TimeAnchor;
            var tick = _strategy.Tick(context.DeltaSeconds);
            var report = _strategy.GetReconciliationReport();

            return new DemoHarnessStepTelemetry(
                tick,
                report,
                _networkStats(),
                _remoteJitter(),
                _acceptedHits(),
                _rejectedHits());
        }
    }
}
