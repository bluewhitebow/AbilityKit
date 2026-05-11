using System;

namespace AbilityKit.Samples.Infrastructure.Config.Attributes
{
    /// <summary>
    /// 行为树动作类型标识属性
    /// 用于标记实现了 IBTAction 接口的动作实现类
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class BTActionTypeIdAttribute : AbilityKit.Core.Common.Marker.MarkerAttribute
    {
        public string ActionName { get; }

        public BTActionTypeIdAttribute(string actionName)
        {
            ActionName = actionName;
        }

        public override void OnScanned(Type implType, AbilityKit.Core.Common.Marker.IMarkerRegistry registry)
        {
            if (registry is BTActionTypeRegistry keyedRegistry)
            {
                keyedRegistry.RegisterByAttribute(this, implType);
            }
        }
    }
}
