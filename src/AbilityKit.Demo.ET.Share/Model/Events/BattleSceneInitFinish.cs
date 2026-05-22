using System;

namespace ET.AbilityKit.Demo.ET.Share
{
    /// <summary>
    /// 战斗场景初始化完成事件
    /// </summary>
    public struct BattleSceneInitFinish: IEvent
    {
        public Type Type => typeof(BattleSceneInitFinish);

        public long PlayerId;
        public string PlayerName;
    }
}
