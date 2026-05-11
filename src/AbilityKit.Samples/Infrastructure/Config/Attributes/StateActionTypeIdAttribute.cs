using System;

namespace AbilityKit.Samples.Infrastructure.Config.Attributes
{
    /// <summary>
    /// 状态机行为类型标识属性
    /// 用于标记状态 Enter/Logic/Exit 时执行的行为类型
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class StateActionTypeIdAttribute : AbilityKit.Core.Common.Marker.MarkerAttribute
    {
        public string ActionName { get; }

        public StateActionTypeIdAttribute(string actionName)
        {
            ActionName = actionName;
        }

        public override void OnScanned(Type implType, AbilityKit.Core.Common.Marker.IMarkerRegistry registry)
        {
            if (registry is StateActionTypeRegistry keyedRegistry)
            {
                keyedRegistry.RegisterByAttribute(this, implType);
            }
        }
    }
}
