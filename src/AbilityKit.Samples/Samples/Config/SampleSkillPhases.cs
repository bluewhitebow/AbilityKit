using AbilityKit.Samples.Infrastructure.Config.Attributes;

namespace AbilityKit.Samples.Samples.Config
{
    /// <summary>
    /// 目标有效性检查阶段
    /// </summary>
    [SkillPhaseTypeId("PreCheck")]
    public sealed class SkillPreCheckPhase
    {
        public bool RequireTarget { get; set; } = true;
        public float MinRange { get; set; } = 5f;
        public float MaxRange { get; set; } = 30f;
    }

    /// <summary>
    /// 资源消耗检查阶段
    /// </summary>
    [SkillPhaseTypeId("CheckCost")]
    public sealed class SkillCheckCostPhase
    {
        public float RequiredMana { get; set; } = 30f;
        public string ResourceType { get; set; } = "Mana";
    }

    /// <summary>
    /// 施法时间阶段
    /// </summary>
    [SkillPhaseTypeId("CastTime")]
    public sealed class SkillCastTimePhase
    {
        public float Duration { get; set; } = 1.5f;
        public string CastAnimation { get; set; }
        public bool CanMove { get; set; } = false;
        public bool CanRotate { get; set; } = true;
    }

    /// <summary>
    /// 效果应用阶段
    /// </summary>
    [SkillPhaseTypeId("ApplyEffect")]
    public sealed class SkillApplyEffectPhase
    {
        public float Damage { get; set; }
        public float EffectRadius { get; set; }
        public string EffectType { get; set; } = "Fire";
    }

    /// <summary>
    /// 冷却阶段
    /// </summary>
    [SkillPhaseTypeId("SkillCooldown")]
    public sealed class SkillCooldownPhase
    {
        public float Duration { get; set; } = 5f;
    }

    /// <summary>
    /// 传送阶段
    /// </summary>
    [SkillPhaseTypeId("Teleport")]
    public sealed class SkillTeleportPhase
    {
        public float TeleportDistance { get; set; } = 15f;
        public bool LeaveEffect { get; set; } = true;
        public bool ArriveEffect { get; set; } = true;
    }
}
