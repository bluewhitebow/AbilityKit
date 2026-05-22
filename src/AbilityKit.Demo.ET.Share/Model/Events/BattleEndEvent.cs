using System;

namespace ET.AbilityKit.Demo.ET.Share
{
    /// <summary>
    /// 战斗结束事件
    /// </summary>
    public struct BattleEndEvent
    {
        public long BattleId;
        public bool IsVictory;
    }
}
