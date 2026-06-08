namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private void CreateRemoteDrivenRuntimeAndWorld()
        {
            var worldRuntime = RemoteDrivenWorldRuntimeFactory.Create(new RemoteDrivenWorldRuntimeFactoryOptions(
                _plan,
                GetFixedDeltaSeconds(),
                ResolveRemoteDrivenInputDelay(),
                _plan.EnableClientPrediction,
                _ => _remoteDrivenConsumable,
                _ => _ctx != null ? _ctx.LocalInputQueue : null,
                ResolveIdealFrameLimit,
                RemoteDrivenRollbackRegistryFactory.Create,
                world => RemoteDrivenStateHashFactory.Create(world, () => DebugForceClientHashMismatch)));

            _handles.RemoteDriven.BindWorldRuntime(worldRuntime);
            RemoteDrivenPredictionContextBinder.Bind(_ctx, _plan, _remoteDrivenRuntime);
            SessionWorldBootstrapValidator.ValidateServices(_remoteDrivenWorld, "RemoteDrivenLocalWorld");
        }

        private void SetupRemoteDrivenInputAndDebugStats()
        {
            _remoteDrivenLastTickedFrame = 0;

            var inputRuntime = RemoteDrivenInputRuntime.Create(ResolveRemoteDrivenInputDelay());
            _handles.RemoteDriven.BindInputRuntime(inputRuntime);
            inputRuntime?.PublishDebugStats();
        }

        private int ResolveRemoteDrivenInputDelay()
        {
            return _plan.InputDelayFrames < 0 ? 0 : _plan.InputDelayFrames;
        }

    }
}
