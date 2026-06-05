using System;
using AbilityKit.Demo.Moba.Share.Config;

namespace AbilityKit.Demo.Moba.Services
{
    // ========================================================================
    // 技能释放条件 DTO
    //
    // 这些 DTO 只描述“是否允许释放技能”的条件配置，用于 Luban 导出和运行时转换。
    // 释放成功后的实际消耗、冷却启动、Buff 等副作用由 SkillHandlerDTO 承载，
    // 并通过 SkillFlowHandlerConfigDTO.PostCastHandlers 执行。
    //
    // 映射入口：SkillConditionDtoConverter。
    // ========================================================================

    /// <summary>
    /// 条件 DTO 空基类。
    /// 所有配置化释放条件都应继承此类，用于 Luban 导出识别和运行时类型路由。
    /// </summary>
    [Serializable]
    public abstract class SkillConditionDTO
    {
        /// <summary>
        /// 条件类型标识，对应 SkillConditionDtoConverter 中注册的类型名。
        /// </summary>
        public string Type;
    }

    // ========================================================================
    // 简单条件 DTO
    // ========================================================================

    /// <summary>
    /// 常量条件 DTO，始终返回固定结果。
    /// </summary>
    [Serializable]
    public class ConstConditionDTO : SkillConditionDTO
    {
        /// <summary>
        /// 常量值。true 表示通过，false 表示失败。
        /// </summary>
        public bool Value = true;

        public ConstConditionDTO()
        {
            Type = "Const";
        }
    }

    /// <summary>
    /// 目标存在条件 DTO。
    /// </summary>
    [Serializable]
    public class HasTargetConditionDTO : SkillConditionDTO
    {
        /// <summary>
        /// 是否取反。true 表示要求没有目标。
        /// </summary>
        public bool Negate;

        public HasTargetConditionDTO()
        {
            Type = "HasTarget";
        }
    }

    // ========================================================================
    // 复合条件 DTO
    // ========================================================================

    /// <summary>
    /// And 组合条件 DTO。
    /// </summary>
    [Serializable]
    public class AndConditionDTO : SkillConditionDTO
    {
        public SkillConditionDTO Left;
        public SkillConditionDTO Right;

        public AndConditionDTO()
        {
            Type = "And";
        }
    }

    /// <summary>
    /// Or 组合条件 DTO。
    /// </summary>
    [Serializable]
    public class OrConditionDTO : SkillConditionDTO
    {
        public SkillConditionDTO Left;
        public SkillConditionDTO Right;

        public OrConditionDTO()
        {
            Type = "Or";
        }
    }

    /// <summary>
    /// Not 条件 DTO。
    /// </summary>
    [Serializable]
    public class NotConditionDTO : SkillConditionDTO
    {
        public SkillConditionDTO Inner;

        public NotConditionDTO()
        {
            Type = "Not";
        }
    }

    /// <summary>
    /// 多条件组合 DTO，支持多个子条件。
    /// </summary>
    [Serializable]
    public class MultiConditionDTO : SkillConditionDTO
    {
        /// <summary>
        /// 组合方式。0 表示 And，1 表示 Or。
        /// </summary>
        public int Combinator;

        /// <summary>
        /// 子条件列表。
        /// </summary>
        public SkillConditionDTO[] Conditions;

        public MultiConditionDTO()
        {
            Type = "Multi";
        }
    }

    /// <summary>
    /// 数值比较条件 DTO。
    /// </summary>
    [Serializable]
    public class NumericCompareConditionDTO : SkillConditionDTO
    {
        /// <summary>
        /// 比较操作符。
        /// </summary>
        public ECompareOp Op;

        /// <summary>
        /// 左操作数。
        /// </summary>
        public NumericRefDTO Left;

        /// <summary>
        /// 右操作数。
        /// </summary>
        public NumericRefDTO Right;

        public NumericCompareConditionDTO()
        {
            Type = "NumericCompare";
        }
    }

    /// <summary>
    /// Payload 字段比较条件 DTO。
    /// </summary>
    [Serializable]
    public class PayloadCompareConditionDTO : SkillConditionDTO
    {
        /// <summary>
        /// Payload 字段 ID。
        /// </summary>
        public int FieldId;

        /// <summary>
        /// 比较操作符。
        /// </summary>
        public ECompareOp Op;

        /// <summary>
        /// 比较值。
        /// </summary>
        public NumericRefDTO CompareValue;

        /// <summary>
        /// 是否取反。
        /// </summary>
        public bool Negate;

        public PayloadCompareConditionDTO()
        {
            Type = "PayloadCompare";
        }
    }

    // ========================================================================
    // Moba 特有条件 DTO
    // ========================================================================

    /// <summary>
    /// 冷却条件 DTO。Moba 特有。
    /// </summary>
    [Serializable]
    public class CooldownConditionDTO : SkillConditionDTO
    {
        public CooldownConditionDTO()
        {
            Type = "Moba_Cooldown";
        }
    }

    /// <summary>
    /// 施法状态条件 DTO。Moba 特有。
    /// </summary>
    [Serializable]
    public class CastingStateConditionDTO : SkillConditionDTO
    {
        /// <summary>
        /// 是否检查正在施法。false 表示检查未在施法。
        /// </summary>
        public bool ExpectCasting;

        public CastingStateConditionDTO()
        {
            Type = "Moba_CastingState";
        }
    }

    /// <summary>
    /// 自身释放条件 DTO。Moba 特有。
    /// </summary>
    [Serializable]
    public class SelfOnlyConditionDTO : SkillConditionDTO
    {
        public SelfOnlyConditionDTO()
        {
            Type = "Moba_SelfOnly";
        }
    }

    /// <summary>
    /// 标签条件 DTO。Moba 特有。
    /// </summary>
    [Serializable]
    public class TagConditionDTO : SkillConditionDTO
    {
        /// <summary>
        /// 需要的标签列表。
        /// </summary>
        public string[] RequiredTags;

        /// <summary>
        /// 忽略的标签列表。
        /// </summary>
        public string[] IgnoreTags;

        /// <summary>
        /// 是否取反。
        /// </summary>
        public bool Negate;

        public TagConditionDTO()
        {
            Type = "Moba_Tag";
        }
    }
}
