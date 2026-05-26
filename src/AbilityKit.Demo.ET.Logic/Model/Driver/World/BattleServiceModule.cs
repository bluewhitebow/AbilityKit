using System;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.DI;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Protocol.Moba;

namespace ET.Logic
{
    /// <summary>
    /// Battle 服务模块
    /// 注册 Battle 世界所需的所有服务
    /// 
    /// 注意：IWorldInputSink 由 MobaWorldBootstrapModule 注册为 MobaLobbyInputSink
    /// 我们需要让 ETMobaInputSink 被使用，所以这里不使用
    /// </summary>
    public sealed class BattleServiceModule : IWorldModule
    {
        public void Configure(WorldContainerBuilder builder)
        {
            // 不再注册 IWorldInputSink，让 MobaWorldBootstrapModule 的 MobaLobbyInputSink 被使用
            // ET Demo 的自定义输入处理逻辑应该在 StartHandler 中通过 IWorldInputSink 接口访问

            Log.Info("[BattleServiceModule] Configured (IWorldInputSink is handled by MobaWorldBootstrapModule)");
        }
    }
}
