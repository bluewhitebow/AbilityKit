using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.AbilityKit.Demo.ET.View
{
    /// <summary>
    /// BattleStartEvent 事件处理器
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class BattleStartEventHandler : AEvent<Scene, BattleStartEvent>
    {
        protected override async ETTask Run(Scene scene, BattleStartEvent args)
        {
            Log.Info($"[BattleStartEventHandler] >>> Battle started: {args.BattleId}");
        }
    }
}
