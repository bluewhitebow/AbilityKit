using System;

namespace AbilityKit.Samples.Infrastructure.Config.Attributes
{
    /// <summary>
    /// 行为树节点类型标识属性
    /// 用于标记实现了 IBTNode 接口的节点类型 (Selector, Sequence, Condition, Action)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class BTNodeTypeIdAttribute : AbilityKit.Core.Common.Marker.MarkerAttribute
    {
        public string NodeType { get; }

        public BTNodeTypeIdAttribute(string nodeType)
        {
            NodeType = nodeType;
        }

        public override void OnScanned(Type implType, AbilityKit.Core.Common.Marker.IMarkerRegistry registry)
        {
            if (registry is BTNodeTypeRegistry keyedRegistry)
            {
                keyedRegistry.RegisterByAttribute(this, implType);
            }
        }
    }
}
