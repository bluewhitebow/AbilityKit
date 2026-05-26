using AbilityKit.Ability.Share.ECS.Entitas;
using AbilityKit.Ability.Triggering.Runtime;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.DI;

namespace AbilityKit.Demo.Moba.Systems.Bootstrap.Flow.Stages
{
    /// <summary>
    /// WorldModules Stage
    /// 注册 Entitas ECS 世界模块和触发器模块
    /// </summary>
    [MobaBootstrapStage]
    public sealed class WorldModulesStage : MobaBootstrapStageBase
    {
        public override string Name => "WorldModules";

        protected internal override void Configure(WorldContainerBuilder builder)
        {
            builder.AddModule(new EntitasEcsWorldModule());
            builder.AddModule(new TriggeringWorldModule());

            // Auto-discover and register all services with [WorldService] attribute
            builder.AddModule(new MobaServicesAutoModule());
        }
    }
}
