using System;
using AbilityKit.Core.Common.Marker;
using AbilityKit.Samples.Infrastructure.Config.Attributes;

namespace AbilityKit.Samples.Infrastructure.Config
{
    /// <summary>
    /// 状态机状态类型注册表
    /// 通过 StateTypeIdAttribute 自动发现和注册状态类型
    /// </summary>
    public sealed class StateTypeRegistry : KeyedMarkerRegistry<string, StateTypeIdAttribute>
    {
        private static readonly Lazy<StateTypeRegistry> _instance = new(() => new StateTypeRegistry());
        public static StateTypeRegistry Instance => _instance.Value;

        private StateTypeRegistry()
        {
            ScanCurrentAssembly();
        }

        private void ScanCurrentAssembly()
        {
            var assembly = typeof(StateTypeRegistry).Assembly;
            MarkerScanner<StateTypeIdAttribute>.Scan(new[] { assembly }, this);
        }

        internal void RegisterByAttribute(StateTypeIdAttribute attr, Type implType)
        {
            if (attr == null || implType == null) return;
            Register(attr.StateName, implType);
        }

        /// <summary>
        /// 根据名称创建状态实例
        /// </summary>
        public object CreateState(string stateName)
        {
            return GetOrCreateInstance(stateName);
        }

        /// <summary>
        /// 尝试根据名称创建状态实例
        /// </summary>
        public bool TryCreateState(string stateName, out object state)
        {
            if (TryGet(stateName, out var type))
            {
                state = Activator.CreateInstance(type);
                return true;
            }
            state = null;
            return false;
        }
    }
}
