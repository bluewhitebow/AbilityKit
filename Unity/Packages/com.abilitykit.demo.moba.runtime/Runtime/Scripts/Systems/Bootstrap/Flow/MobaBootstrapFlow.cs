using System;
using System.Collections.Generic;
using AbilityKit.Ability.Share.ECS.Entitas;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World;
using AbilityKit.Core.Common.Log;

namespace AbilityKit.Demo.Moba.Systems.Bootstrap.Flow
{
    /// <summary>
    /// Moba Bootstrap Flow
    /// 使用 Flow 模式管理引导阶段
    /// 替代旧的 partial class 系统
    /// </summary>
    public sealed class MobaBootstrapFlow : IWorldModule, IEntitasSystemsInstaller
    {
        public const int InitOpCode = 2000;

        private readonly IEnumerable<MobaBootstrapStageBase> _configureStages;
        private readonly IEnumerable<MobaBootstrapStageBase> _installStages;

        public MobaBootstrapFlow()
        {
            _configureStages = MobaBootstrapStageRegistry.GetConfigureStages();
            _installStages = MobaBootstrapStageRegistry.GetInstallStages();
        }

        public void Configure(WorldContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            Log.Info($"[MobaBootstrapFlow] Configure: starting {_configureStages.GetEnumerator().Current} stages");

            foreach (var stage in _configureStages)
            {
                stage.ExecuteConfigure(builder);
            }

            Log.Info("[MobaBootstrapFlow] Configure: all stages completed");
        }

        public void Install(global::Entitas.IContexts contexts, global::Entitas.Systems systems, IWorldResolver services)
        {
            Log.Info("[MobaBootstrapFlow] Install: begin");
            if (contexts == null) throw new ArgumentNullException(nameof(contexts));
            if (systems == null) throw new ArgumentNullException(nameof(systems));
            if (services == null) throw new ArgumentNullException(nameof(services));

            Log.Info($"[MobaBootstrapFlow] Install: starting {_installStages} stages");

            foreach (var stage in _installStages)
            {
                stage.ExecuteInstall(contexts, systems, services);
            }

            Log.Info("[MobaBootstrapFlow] Install: all stages completed");
        }
    }
}
