using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.AbilityKit.Demo.ET.View
{
    /// <summary>
    /// ActorDeadEvent 事件处理器
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ActorDeadEventHandler : AEvent<Scene, ActorDeadEvent>
    {
        protected override async ETTask Run(Scene scene, ActorDeadEvent args)
        {
            Log.Info($"[ActorDeadEventHandler] >>> Actor dead: {args.ActorId}, Killer: {args.KillerId}");
        }
    }
}
