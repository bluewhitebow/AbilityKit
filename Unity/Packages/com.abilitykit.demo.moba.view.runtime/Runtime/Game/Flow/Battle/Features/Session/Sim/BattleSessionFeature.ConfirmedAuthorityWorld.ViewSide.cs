using System;
using System.Collections.Generic;
using UnityEngine;
using AbilityKit.Core.Common.SnapshotRouting;
using AbilityKit.Game.Battle.Component;
using AbilityKit.Game.Battle.Entity;
using AbilityKit.Game.Flow.Battle.ViewEvents;
using AbilityKit.Protocol.Moba;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.World.ECS;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private void EnsureConfirmedAuthorityViewSide(WorldId authWorldId)
        {
            if (ShouldCreateConfirmedAuthorityViewSide())
            {
                var viewSide = ConfirmedViewSideRuntimeFactory.Create(_ctx, authWorldId);
                _confirmedViewCtx = viewSide.Context;
                _confirmedViewSnapshotRuntime = viewSide.SnapshotRuntime;
                _confirmedViewFeature = viewSide.Feature;
                AttachConfirmedViewFeature(_confirmedViewFeature);
            }

            ConfirmedAuthorityDebugStatsPublisher.Initialize(authWorldId);
        }

        private bool ShouldCreateConfirmedAuthorityViewSide()
        {
            return _flow != null && _confirmedViewFeature == null && _plan.EnableConfirmedAuthorityWorld;
        }

        private void AttachConfirmedViewFeature(ConfirmedBattleViewFeature feature)
        {
            if (_flow == null || feature == null) return;

            _flow.Attach(feature);
        }

    }
}

