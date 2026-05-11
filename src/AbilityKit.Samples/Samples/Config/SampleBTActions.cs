using AbilityKit.Samples.Infrastructure.Config.Attributes;

namespace AbilityKit.Samples.Samples.Config
{
    /// <summary>
    /// 看向目标动作
    /// </summary>
    [BTActionTypeId("LookAt")]
    public sealed class LookAtAction
    {
        public float RotationSpeed { get; set; } = 360f;
    }

    /// <summary>
    /// 移动到目标动作
    /// </summary>
    [BTActionTypeId("MoveTo")]
    public sealed class MoveToAction
    {
        public float Distance { get; set; } = 2f;
        public float MoveSpeed { get; set; } = 5f;
    }

    /// <summary>
    /// 移动到巡逻点动作
    /// </summary>
    [BTActionTypeId("MoveToPatrolPoint")]
    public sealed class MoveToPatrolPointAction
    {
        public float Speed { get; set; } = 2f;
    }

    /// <summary>
    /// 在巡逻点等待动作
    /// </summary>
    [BTActionTypeId("WaitAtPoint")]
    public sealed class WaitAtPointAction
    {
        public float Duration { get; set; } = 3f;
    }

    /// <summary>
    /// 攻击动作
    /// </summary>
    [BTActionTypeId("Attack")]
    public sealed class BTAttackAction
    {
        public float Damage { get; set; } = 50f;
        public float Range { get; set; } = 1.5f;
    }

    /// <summary>
    /// 施放技能动作
    /// </summary>
    [BTActionTypeId("CastSkill")]
    public sealed class CastSkillAction
    {
        public string SkillId { get; set; }
        public float CastTime { get; set; } = 1f;
    }
}
