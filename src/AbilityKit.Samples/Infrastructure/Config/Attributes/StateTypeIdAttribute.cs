using System;

namespace AbilityKit.Samples.Infrastructure.Config.Attributes
{
    /// <summary>
    /// 状态机状态类型标识属性
    /// 用于标记实现了 IState 接口的状态类型
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class StateTypeIdAttribute : AbilityKit.Core.Common.Marker.MarkerAttribute
    {
        public string StateName { get; }

        public StateTypeIdAttribute(string stateName)
        {
            StateName = stateName;
        }

        public override void OnScanned(Type implType, AbilityKit.Core.Common.Marker.IMarkerRegistry registry)
        {
            if (registry is StateTypeRegistry keyedRegistry)
            {
                keyedRegistry.RegisterByAttribute(this, implType);
            }
        }
    }
}
