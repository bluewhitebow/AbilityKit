using System;

namespace ET.AbilityKit.Demo.ET.Share
{
    /// <summary>
    /// 单位受伤事件
    /// </summary>
    public struct ActorDamageEvent
    {
        public long ActorId;
        public long SourceActorId;
        public float Damage;
        public float CurrentHp;
        public float MaxHp;
    }
}
