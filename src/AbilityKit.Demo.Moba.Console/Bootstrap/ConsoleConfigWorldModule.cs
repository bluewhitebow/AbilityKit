using System;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;

namespace AbilityKit.Demo.Moba.Console.Bootstrap
{
    /// <summary>
    /// Console 环境的配置模块。
    /// 使用 AttributeWorldServicesModule 自动扫描并注册所有 [WorldService] 标记的类型。
    /// </summary>
    public sealed class ConsoleConfigWorldModule : IWorldModule
    {
        public void Configure(WorldContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            // 自动扫描并注册所有 [WorldService] 标记的类型
            var attrModule = new AttributeWorldServicesModule(
                WorldServiceProfile.All,
                scanAllLoadedAssemblies: true,
                namespacePrefixes: new[] { "AbilityKit.Demo.Moba.Console" });

            attrModule.Configure(builder);
        }
    }
}
