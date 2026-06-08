using System;
using AbilityKit.Ability.World.Abstractions;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private void TickConfirmedAuthorityWorldSim(float deltaTime)
        {
            if (_confirmedWorld == null || _confirmedRuntime == null) return;
            if (_confirmedInputSource == null) return;

            var inputTargetFrame = _confirmedInputSource.TargetFrame;
            if (inputTargetFrame <= 0) return;

            var driveTargetFrame = inputTargetFrame;
            var confirmedFrame = 0;
            var predictedFrame = 0;

            var stats = _ctx != null ? _ctx.PredictionStats : null;
            if (stats != null)
            {
                var wid = new WorldId(_plan.WorldId);
                if (stats.TryGetFrames(wid, out var confirmed, out var predicted))
                {
                    confirmedFrame = confirmed.Value;
                    predictedFrame = predicted.Value;

                    if (confirmedFrame > 0)
                    {
                        driveTargetFrame = Math.Min(inputTargetFrame, confirmedFrame);
                    }
                }
            }

            if (driveTargetFrame <= 0) return;

            var fixedDelta = GetFixedDeltaSeconds();
            var stepsBudget = MaxRemoteDrivenCatchUpStepsPerUpdate;
            if (stepsBudget <= 0) return;

            _confirmedLastTickedFrame = _worldCatchUp.CatchUpAndFeedSnapshots(
                runtime: _confirmedRuntime,
                world: _confirmedWorld,
                lastTickedFrame: _confirmedLastTickedFrame,
                driveTargetFrame: driveTargetFrame,
                fixedDelta: fixedDelta,
                stepsBudget: stepsBudget,
                feed: packet =>
                {
                    _confirmedSnapshots?.Feed(packet);
                    _confirmedViewSnapshotRuntime?.Snapshots?.Feed(packet);
                });

            _confirmedInputSource.TrimBefore(_confirmedLastTickedFrame - 120);

            ConfirmedAuthorityDebugStatsPublisher.Update(
                confirmedFrame,
                predictedFrame,
                inputTargetFrame,
                driveTargetFrame,
                _confirmedLastTickedFrame,
                _confirmedViewEventSink);
        }
    }
}
