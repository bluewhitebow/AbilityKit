using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.AbilityKit.Demo.View
{
    /// <summary>
    /// ActorMoveEvent 事件处理器
    /// 订阅 ET.Logic 发布的事件，更新视图层数据
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ActorMoveEventHandler : AEvent<Scene, ActorMoveEvent>
    {
        protected override async ETTask Run(Scene scene, ActorMoveEvent args)
        {
            var listener = scene.GetComponent<ETViewEventListener>();
            if (listener != null)
            {
                var view = listener.GetUnitView((int)args.ActorId);
                if (view != null)
                {
                    view.UpdatePosition(args.X, args.Y);
                    view.UpdateRotation(args.Rotation);
                    Log.Info($"[ActorMoveEventHandler] Unit {view.Name} moved to ({args.X:F2}, {args.Y:F2}), rotation={args.Rotation:F2}");
                    return;
                }
            }

            Log.Info($"[ActorMoveEventHandler] View not found for ActorId={args.ActorId}");
        }
    }
}
