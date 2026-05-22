using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.AbilityKit.Demo.ET.View
{
    /// <summary>
    /// ActorSpawnEvent 事件处理器
    /// 监听逻辑层派发的单位生成事件
    /// 注意：实际的视图创建由 Logic 层的 ETBattleViewComponent 处理
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ActorSpawnEventHandler : AEvent<Scene, ActorSpawnEvent>
    {
        protected override async ETTask Run(Scene scene, ActorSpawnEvent args)
        {
            Log.Info($"[ActorSpawnEventHandler] >>> Received ActorSpawnEvent: {args.Name} ({args.ActorId})");

            // 视图创建由 Logic 层的 ETBattleViewComponent.AddUnitView 处理
            // 这里只记录日志
            Log.Info($"[ActorSpawnEventHandler] >>> Actor spawned in view: {args.Name} ({args.ActorId}), Kind={args.Kind}, IsLocalPlayer={args.IsLocalPlayer}");
        }
    }
}
