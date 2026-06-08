using AbilityKit.Ability.World.Abstractions;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private void CreateConfirmedAuthorityRuntimeAndWorld(out WorldId authWorldId)
        {
            var worldRuntime = ConfirmedAuthorityWorldRuntimeFactory.Create(
                _plan,
                GetFixedDeltaSeconds(),
                _ => _confirmedConsumable,
                ResolveIdealFrameLimit);

            _handles.Confirmed.BindWorldRuntime(worldRuntime);
            authWorldId = worldRuntime.WorldId;
        }

        private void SetupConfirmedAuthorityInputAndBootstrap()
        {
            _confirmedLastTickedFrame = 0;

            var inputRuntime = ConfirmedAuthorityInputRuntime.Create();
            _handles.Confirmed.BindInputRuntime(inputRuntime);
            SessionWorldBootstrapValidator.ValidateServices(_confirmedWorld, "ConfirmedAuthorityWorld");
        }
    }
}
