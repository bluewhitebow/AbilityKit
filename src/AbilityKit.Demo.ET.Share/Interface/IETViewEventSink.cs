using System;

namespace ET.AbilityKit.Demo.ET.Share
{
    /// <summary>
    /// ET Demo 常量
    /// </summary>
    public static class DemoConstants
    {
        public const string Name = "ETDemo";
        public const string Version = "1.0.0";
    }

    /// <summary>
    /// ET View 事件接口
    /// </summary>
    public interface IETViewEventSink
    {
        void OnActorSpawn(long actorId, string name, float x, float y);
        void OnActorMove(long actorId, float x, float y);
        void OnActorDamage(long actorId, float damage, float hpAfter, float maxHp);
        void OnActorDead(long actorId);
        void OnSkillCast(long casterId, int skillId, float targetX, float targetY);
    }

    /// <summary>
    /// ET 输入接口
    /// </summary>
    public interface IETInputSink
    {
        void SubmitMoveInput(long actorId, float x, float y);
        void SubmitSkillInput(long actorId, int skillSlot, float targetX, float targetY);
    }

    /// <summary>
    /// Actor 基础数据
    /// </summary>
    public struct ActorData
    {
        public long ActorId;
        public string Name;
        public float X;
        public float Y;
        public float Hp;
        public float MaxHp;

        public ActorData(long actorId, string name, float x, float y)
        {
            ActorId = actorId;
            Name = name;
            X = x;
            Y = y;
            Hp = 100f;
            MaxHp = 100f;
        }
    }

    /// <summary>
    /// 技能数据
    /// </summary>
    public struct SkillData
    {
        public int SkillId;
        public string Name;
        public float Cooldown;
        public float Range;

        public SkillData(int skillId, string name, float cooldown, float range)
        {
            SkillId = skillId;
            Name = name;
            Cooldown = cooldown;
            Range = range;
        }
    }
}
