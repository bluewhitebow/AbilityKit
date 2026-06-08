using AbilityKit.Core.Common.SnapshotRouting;
using AbilityKit.Game.Flow.Battle.ViewEvents;
using AbilityKit.Game.Flow.Battle.ViewEvents.Snapshot;
using AbilityKit.Game.Flow.Battle.ViewEvents.Triggering;

using System;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Triggering;
using AbilityKit.Protocol.Moba;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private void EnsureConfirmedAuthorityViewEventPipeline()
        {
            if (_session == null) return;

            _confirmedViewEventPipeline = ConfirmedViewEventPipelineFactory.Create(
                _confirmedWorld,
                _plan.ViewEventSourceMode,
                maxDebugLines: 32);

            _confirmedSnapshots = _confirmedViewEventPipeline.Snapshots;
            _confirmedViewEventSink = _confirmedViewEventPipeline.EventSink;
            _confirmedSnapshotViewAdapter = _confirmedViewEventPipeline.SnapshotViewAdapter;
            _confirmedTriggerBridge = _confirmedViewEventPipeline.TriggerBridge;
        }

    }
}
