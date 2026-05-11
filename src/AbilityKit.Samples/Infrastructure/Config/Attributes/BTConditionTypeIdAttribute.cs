using System;

namespace AbilityKit.Samples.Infrastructure.Config.Attributes
{
    /// <summary>
    /// 行为树条件类型标识属性
    /// 用于标记实现了 IBTCondition 接口的条件实现类
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class BTConditionTypeIdAttribute : AbilityKit.Core.Common.Marker.MarkerAttribute
    {
        public string ConditionName { get; }

        public BTConditionTypeIdAttribute(string conditionName)
        {
            ConditionName = conditionName;
        }

        public override void OnScanned(Type implType, AbilityKit.Core.Common.Marker.IMarkerRegistry registry)
        {
            if (registry is BTConditionTypeRegistry keyedRegistry)
            {
                keyedRegistry.RegisterByAttribute(this, implType);
            }
        }
    }
}
