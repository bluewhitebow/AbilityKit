using System;

namespace ET.AbilityKit.Demo.ET.Share
{
    /// <summary>
    /// 技能释放事件
    /// </summary>
    public struct SkillCastEvent
    {
        public long CasterActorId;
        public int SkillId;
        public float TargetX;
        public float TargetY;
    }
}
