using AbilityKit.Samples.Infrastructure.Config.Attributes;

namespace AbilityKit.Samples.Samples.Config
{
    /// <summary>
    /// 检测范围内是否有目标
    /// </summary>
    [BTConditionTypeId("HasTargetInRange")]
    public sealed class HasTargetInRangeCondition
    {
        public float Range { get; set; } = 10f;
    }

    /// <summary>
    /// 是否在攻击范围内
    /// </summary>
    [BTConditionTypeId("IsInAttackRange")]
    public sealed class IsInAttackRangeCondition
    {
        public float AttackRange { get; set; } = 1.5f;
    }

    /// <summary>
    /// 没有目标
    /// </summary>
    [BTConditionTypeId("NoTarget")]
    public sealed class NoTargetCondition { }

    /// <summary>
    /// 目标是否存活
    /// </summary>
    [BTConditionTypeId("IsTargetAlive")]
    public sealed class IsTargetAliveCondition { }

    /// <summary>
    /// 是否有足够资源
    /// </summary>
    [BTConditionTypeId("HasEnoughResource")]
    public sealed class HasEnoughResourceCondition
    {
        public float Cost { get; set; } = 20f;
    }

    /// <summary>
    /// 是否在冷却中
    /// </summary>
    [BTConditionTypeId("IsOnCooldown")]
    public sealed class IsOnCooldownCondition
    {
        public string SkillId { get; set; }
    }
}
