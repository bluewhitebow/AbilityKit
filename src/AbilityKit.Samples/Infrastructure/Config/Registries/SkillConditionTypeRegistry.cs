using System;
using AbilityKit.Core.Common.Marker;
using AbilityKit.Samples.Infrastructure.Config.Attributes;

namespace AbilityKit.Samples.Infrastructure.Config
{
    /// <summary>
    /// 技能条件类型注册表
    /// 通过 SkillConditionTypeIdAttribute 自动发现和注册技能条件类型 (HasEnoughMana, TargetInRange 等)
    /// </summary>
    public sealed class SkillConditionTypeRegistry : KeyedMarkerRegistry<string, SkillConditionTypeIdAttribute>
    {
        private static readonly Lazy<SkillConditionTypeRegistry> _instance = new(() => new SkillConditionTypeRegistry());
        public static SkillConditionTypeRegistry Instance => _instance.Value;

        private SkillConditionTypeRegistry()
        {
            ScanCurrentAssembly();
        }

        private void ScanCurrentAssembly()
        {
            var assembly = typeof(SkillConditionTypeRegistry).Assembly;
            MarkerScanner<SkillConditionTypeIdAttribute>.Scan(new[] { assembly }, this);
        }

        internal void RegisterByAttribute(SkillConditionTypeIdAttribute attr, Type implType)
        {
            if (attr == null || implType == null) return;
            Register(attr.ConditionName, implType);
        }

        /// <summary>
        /// 根据条件名称创建技能条件实例
        /// </summary>
        public object CreateCondition(string conditionName)
        {
            return GetOrCreateInstance(conditionName);
        }
    }
}
