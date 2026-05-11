using AbilityKit.Samples.Infrastructure.Config.Attributes;

namespace AbilityKit.Samples.Samples.Config
{
    /// <summary>
    /// 是否有足够的魔法值
    /// </summary>
    [SkillConditionTypeId("HasEnoughMana")]
    public sealed class HasEnoughManaCondition
    {
        public float RequiredMana { get; set; } = 30f;
    }

    /// <summary>
    /// 目标是否在范围内
    /// </summary>
    [SkillConditionTypeId("TargetInRange")]
    public sealed class TargetInRangeCondition
    {
        public float MinRange { get; set; } = 5f;
        public float MaxRange { get; set; } = 30f;
    }

    /// <summary>
    /// 是否不在冷却中
    /// </summary>
    [SkillConditionTypeId("NotOnCooldown")]
    public sealed class NotOnCooldownCondition
    {
        public string SkillId { get; set; }
    }

    /// <summary>
    /// 目标是否有效
    /// </summary>
    [SkillConditionTypeId("TargetValid")]
    public sealed class TargetValidCondition { }

    /// <summary>
    /// 是否被沉默
    /// </summary>
    [SkillConditionTypeId("NotSilenced")]
    public sealed class NotSilencedCondition { }

    /// <summary>
    /// 是否有足够的生命值
    /// </summary>
    [SkillConditionTypeId("HasEnoughHealth")]
    public sealed class HasEnoughHealthCondition
    {
        public float RequiredHealthPercent { get; set; } = 0.3f;
    }
}
