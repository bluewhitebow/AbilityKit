using System;
using AbilityKit.Ability.Host;
using AbilityKit.Core.Common.SnapshotRouting;
using AbilityKit.Game.Battle;
using AbilityKit.Game.Flow.Battle.ViewEvents;
using AbilityKit.Game.Flow.Battle.ViewEvents.Snapshot;
using AbilityKit.Game.Flow.Battle.ViewEvents.Triggering;
using AbilityKit.Network.Abstractions;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private AbilityKit.Ability.Host.Framework.HostRuntime _confirmedRuntime
        {
            get => _handles.Confirmed.Runtime;
            set => _handles.Confirmed.Runtime = value;
        }

        private AbilityKit.Ability.World.Abstractions.IWorld _confirmedWorld
        {
            get => _handles.Confirmed.World;
            set => _handles.Confirmed.World = value;
        }

        private IRemoteFrameSource<PlayerInputCommand[]> _confirmedInputSource
        {
            get => _handles.Confirmed.InputSource;
            set => _handles.Confirmed.InputSource = value;
        }

        private IConsumableRemoteFrameSource<PlayerInputCommand[]> _confirmedConsumable
        {
            get => _handles.Confirmed.Consumable;
            set => _handles.Confirmed.Consumable = value;
        }

        private IRemoteFrameSink<PlayerInputCommand[]> _confirmedSink
        {
            get => _handles.Confirmed.Sink;
            set => _handles.Confirmed.Sink = value;
        }

        private ConfirmedViewEventPipeline _confirmedViewEventPipeline
        {
            get => _handles.Confirmed.ViewEventPipeline;
            set => _handles.Confirmed.ViewEventPipeline = value;
        }

        private FrameSnapshotDispatcher _confirmedSnapshots
        {
            get => _handles.Confirmed.Snapshots;
            set => _handles.Confirmed.Snapshots = value;
        }

        private DebugBattleViewEventSink _confirmedViewEventSink
        {
            get => _handles.Confirmed.ViewEventSink;
            set => _handles.Confirmed.ViewEventSink = value;
        }

        private BattleSnapshotViewAdapter _confirmedSnapshotViewAdapter
        {
            get => _handles.Confirmed.SnapshotViewAdapter;
            set => _handles.Confirmed.SnapshotViewAdapter = value;
        }

        private BattleTriggerEventViewBridge _confirmedTriggerBridge
        {
            get => _handles.Confirmed.TriggerBridge;
            set => _handles.Confirmed.TriggerBridge = value;
        }

        private BattleContext _confirmedViewCtx
        {
            get => _handles.Confirmed.ViewCtx;
            set => _handles.Confirmed.ViewCtx = value;
        }

        private ConfirmedViewSnapshotRuntime _confirmedViewSnapshotRuntime
        {
            get => _handles.Confirmed.ViewSnapshotRuntime;
            set => _handles.Confirmed.ViewSnapshotRuntime = value;
        }

        private ConfirmedBattleViewFeature _confirmedViewFeature
        {
            get => _handles.Confirmed.ViewFeature;
            set => _handles.Confirmed.ViewFeature = value;
        }

        private void StartConfirmedAuthorityWorld()
        {
            if (_confirmedWorld != null) return;

            CreateConfirmedAuthorityRuntimeAndWorld(out var authWorldId);
            SetupConfirmedAuthorityInputAndBootstrap();

            EnsureConfirmedAuthorityViewEventPipeline();

            // Build a dedicated view context for confirmed authority world and attach an extra view feature.
            // This context owns its own EC.IECWorld and view binder mappings, isolated from the main battle context.
            EnsureConfirmedAuthorityViewSide(authWorldId);
        }
    }
}
