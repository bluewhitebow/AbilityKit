using System;
using AbilityKit.Core.Common.Marker;
using AbilityKit.Samples.Infrastructure.Config.Attributes;

namespace AbilityKit.Samples.Infrastructure.Config
{
    /// <summary>
    /// 行为树动作类型注册表
    /// 通过 BTActionTypeIdAttribute 自动发现和注册动作实现类型 (LookAt, MoveTo, Patrol 等)
    /// </summary>
    public sealed class BTActionTypeRegistry : KeyedMarkerRegistry<string, BTActionTypeIdAttribute>
    {
        private static readonly Lazy<BTActionTypeRegistry> _instance = new(() => new BTActionTypeRegistry());
        public static BTActionTypeRegistry Instance => _instance.Value;

        private BTActionTypeRegistry()
        {
            ScanCurrentAssembly();
        }

        private void ScanCurrentAssembly()
        {
            var assembly = typeof(BTActionTypeRegistry).Assembly;
            MarkerScanner<BTActionTypeIdAttribute>.Scan(new[] { assembly }, this);
        }

        internal void RegisterByAttribute(BTActionTypeIdAttribute attr, Type implType)
        {
            if (attr == null || implType == null) return;
            Register(attr.ActionName, implType);
        }

        /// <summary>
        /// 根据动作名称创建动作实例
        /// </summary>
        public object CreateAction(string actionName)
        {
            return GetOrCreateInstance(actionName);
        }
    }
}
