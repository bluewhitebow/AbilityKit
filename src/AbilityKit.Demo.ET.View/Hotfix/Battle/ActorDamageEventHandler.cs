using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.AbilityKit.Demo.ET.View
{
    /// <summary>
    /// ActorDamageEvent 事件处理器
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ActorDamageEventHandler : AEvent<Scene, ActorDamageEvent>
    {
        protected override async ETTask Run(Scene scene, ActorDamageEvent args)
        {
            Log.Info($"[ActorDamageEventHandler] >>> Actor damaged: {args.ActorId}, Damage: {args.Damage}, CurrentHP: {args.CurrentHp}/{args.MaxHp}");
        }
    }
}
