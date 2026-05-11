using System;
using AbilityKit.Core.Common.Marker;
using AbilityKit.Samples.Infrastructure.Config.Attributes;

namespace AbilityKit.Samples.Infrastructure.Config
{
    /// <summary>
    /// 状态机行为类型注册表
    /// 通过 StateActionTypeIdAttribute 自动发现和注册状态行为类型
    /// </summary>
    public sealed class StateActionTypeRegistry : KeyedMarkerRegistry<string, StateActionTypeIdAttribute>
    {
        private static readonly Lazy<StateActionTypeRegistry> _instance = new(() => new StateActionTypeRegistry());
        public static StateActionTypeRegistry Instance => _instance.Value;

        private StateActionTypeRegistry()
        {
            ScanCurrentAssembly();
        }

        private void ScanCurrentAssembly()
        {
            var assembly = typeof(StateActionTypeRegistry).Assembly;
            MarkerScanner<StateActionTypeIdAttribute>.Scan(new[] { assembly }, this);
        }

        internal void RegisterByAttribute(StateActionTypeIdAttribute attr, Type implType)
        {
            if (attr == null || implType == null) return;
            Register(attr.ActionName, implType);
        }

        /// <summary>
        /// 根据名称创建行为实例
        /// </summary>
        public object CreateAction(string actionName)
        {
            return GetOrCreateInstance(actionName);
        }
    }
}
