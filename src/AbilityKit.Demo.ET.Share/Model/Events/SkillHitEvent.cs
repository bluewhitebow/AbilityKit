using System;

namespace ET.AbilityKit.Demo.ET.Share
{
    /// <summary>
    /// 技能命中事件
    /// </summary>
    public struct SkillHitEvent
    {
        public long TargetActorId;
        public long CasterActorId;
        public int SkillId;
        public float Damage;
    }
}
