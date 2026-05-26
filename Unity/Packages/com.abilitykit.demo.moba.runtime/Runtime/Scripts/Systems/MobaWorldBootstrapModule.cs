using System;
using AbilityKit.Ability.Share.ECS.Entitas;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World;
using AbilityKit.Demo.Moba.Systems.Bootstrap.Flow;

namespace AbilityKit.Demo.Moba.Systems
{
    /// <summary>
    /// Moba World Bootstrap Module
    /// 委托给新的 Flow Bootstrap 系统
    /// </summary>
    public sealed partial class MobaWorldBootstrapModule : IWorldModule, IEntitasSystemsInstaller
    {
        public const int InitOpCode = 2000;

        private static readonly MobaBootstrapFlow _flowBootstrap;

        static MobaWorldBootstrapModule()
        {
            // 触发静态初始化，确保所有 Stage 被注册
            MobaBootstrapFlowModule.EnsureInitialized();
            _flowBootstrap = new MobaBootstrapFlow();
        }

        public void Configure(WorldContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            _flowBootstrap.Configure(builder);
        }

        public void Install(global::Entitas.IContexts contexts, global::Entitas.Systems systems, IWorldResolver services)
        {
            if (contexts == null) throw new ArgumentNullException(nameof(contexts));
            if (systems == null) throw new ArgumentNullException(nameof(systems));
            if (services == null) throw new ArgumentNullException(nameof(services));

            // 安装 Entitas ECS 系统
            AutoSystemInstaller.Install(
                contexts,
                systems,
                services,
                assemblies: new[] { typeof(MobaWorldBootstrapModule).Assembly, typeof(AbilityKit.Core.Common.Projectile.ProjectileTickSystem).Assembly },
                namespacePrefixes: new[]
                {
                    "AbilityKit.Demo.Moba",
                    "AbilityKit.Core.Common.Projectile",
                }
            );

            // 执行 Flow Bootstrap Install
            _flowBootstrap.Install(contexts, systems, services);
        }
    }
}

