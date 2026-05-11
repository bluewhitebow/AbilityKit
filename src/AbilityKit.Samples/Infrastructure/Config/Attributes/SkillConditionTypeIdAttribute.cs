using System;

namespace AbilityKit.Samples.Infrastructure.Config.Attributes
{
    /// <summary>
    /// 技能条件类型标识属性
    /// 用于标记实现了 ISkillCondition 接口的条件类型 (HasEnoughMana, TargetInRange 等)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class SkillConditionTypeIdAttribute : AbilityKit.Core.Common.Marker.MarkerAttribute
    {
        public string ConditionName { get; }

        public SkillConditionTypeIdAttribute(string conditionName)
        {
            ConditionName = conditionName;
        }

        public override void OnScanned(Type implType, AbilityKit.Core.Common.Marker.IMarkerRegistry registry)
        {
            if (registry is SkillConditionTypeRegistry keyedRegistry)
            {
                keyedRegistry.RegisterByAttribute(this, implType);
            }
        }
    }
}
