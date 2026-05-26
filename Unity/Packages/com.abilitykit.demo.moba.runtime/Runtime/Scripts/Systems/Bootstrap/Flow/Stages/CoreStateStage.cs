using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Demo.Moba.Rollback;

namespace AbilityKit.Demo.Moba.Systems.Bootstrap.Flow.Stages
{
    /// <summary>
    /// CoreState Stage
    /// 注册核心状态服务
    /// </summary>
    [MobaBootstrapStage]
    public sealed class CoreStateStage : MobaBootstrapStageBase
    {
        public override string Name => "CoreState";

        protected internal override void Configure(WorldContainerBuilder builder)
        {
            // Deterministic + rollbackable RNG (override default world random)
            builder.Register<IWorldRandom>(WorldLifetime.Scoped, _ => new RollbackWorldRandom());
        }
    }
}
