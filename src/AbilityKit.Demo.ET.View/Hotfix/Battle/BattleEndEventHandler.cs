using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.AbilityKit.Demo.ET.View
{
    /// <summary>
    /// BattleEndEvent 事件处理器
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class BattleEndEventHandler : AEvent<Scene, BattleEndEvent>
    {
        protected override async ETTask Run(Scene scene, BattleEndEvent args)
        {
            Log.Info($"[BattleEndEventHandler] >>> Battle ended: {args.BattleId}, Victory: {args.IsVictory}");
        }
    }
}
