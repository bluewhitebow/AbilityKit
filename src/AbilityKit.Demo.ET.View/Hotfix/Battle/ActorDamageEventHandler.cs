using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.AbilityKit.Demo.View
{
    /// <summary>
    /// ActorDamageEvent 事件处理器
    /// 订阅 ET.Logic 发布的事件，更新视图层数据
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ActorDamageEventHandler : AEvent<Scene, ActorDamageEvent>
    {
        protected override async ETTask Run(Scene scene, ActorDamageEvent args)
        {
            var listener = scene.GetComponent<ETViewEventListener>();
            if (listener != null)
            {
                var view = listener.GetUnitView((int)args.ActorId);
                if (view != null)
                {
                    view.UpdateHp(args.CurrentHp, args.MaxHp);
                    view.ShowDamage(args.Damage);
                    Log.Debug($"[ActorDamageEventHandler] Unit {view.Name} took {args.Damage} damage, HP: {args.CurrentHp}/{args.MaxHp}");
                    return;
                }
            }

            Log.Debug($"[ActorDamageEventHandler] View not found for ActorId={args.ActorId}");
        }
    }
}
