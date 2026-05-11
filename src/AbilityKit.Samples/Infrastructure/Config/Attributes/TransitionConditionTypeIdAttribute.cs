using System;

namespace AbilityKit.Samples.Infrastructure.Config.Attributes
{
    /// <summary>
    /// 状态转换条件类型标识属性
    /// 用于标记实现了 ITransitionCondition 接口的条件类型
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class TransitionConditionTypeIdAttribute : AbilityKit.Core.Common.Marker.MarkerAttribute
    {
        public string ConditionName { get; }

        public TransitionConditionTypeIdAttribute(string conditionName)
        {
            ConditionName = conditionName;
        }

        public override void OnScanned(Type implType, AbilityKit.Core.Common.Marker.IMarkerRegistry registry)
        {
            if (registry is TransitionConditionTypeRegistry keyedRegistry)
            {
                keyedRegistry.RegisterByAttribute(this, implType);
            }
        }
    }
}
