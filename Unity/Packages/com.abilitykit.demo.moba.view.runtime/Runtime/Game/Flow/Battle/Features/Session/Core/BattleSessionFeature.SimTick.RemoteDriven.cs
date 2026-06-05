using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private void TickRemoteDrivenLocalSim(float deltaTime)
        {
            if (_remoteDrivenWorld == null || _remoteDrivenRuntime == null) return;
            if (_remoteDrivenInputSource == null) return;

            var inputTargetFrame = _remoteDrivenInputSource.TargetFrame;
            if (inputTargetFrame <= 0) return;

            var driveTargetFrame = inputTargetFrame;

            _remoteDrivenInputSource.DelayFrames = _plan.InputDelayFrames < 0 ? 0 : _plan.InputDelayFrames;

            if (driveTargetFrame <= 0) return;

            var fixedDelta = GetFixedDeltaSeconds();
            var stepsBudget = MaxRemoteDrivenCatchUpStepsPerUpdate;
            if (stepsBudget <= 0) return;

            _remoteDrivenLastTickedFrame = _worldCatchUp.CatchUpAndFeedSnapshots(
                runtime: _remoteDrivenRuntime,
                world: _remoteDrivenWorld,
                lastTickedFrame: _remoteDrivenLastTickedFrame,
                driveTargetFrame: driveTargetFrame,
                fixedDelta: fixedDelta,
                stepsBudget: stepsBudget,
                feed: packet => _snapshots?.Feed(packet));

            _remoteDrivenInputSource.TrimBefore(_remoteDrivenLastTickedFrame - 120);
        }
    }
}
