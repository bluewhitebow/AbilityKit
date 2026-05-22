using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.AbilityKit.Demo.ET.View
{
    /// <summary>
    /// ActorMoveEvent 事件处理器
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ActorMoveEventHandler : AEvent<Scene, ActorMoveEvent>
    {
        protected override async ETTask Run(Scene scene, ActorMoveEvent args)
        {
            Log.Info($"[ActorMoveEventHandler] >>> Received: ActorId={args.ActorId}, Pos=({args.X}, {args.Y})");
        }
    }
}
