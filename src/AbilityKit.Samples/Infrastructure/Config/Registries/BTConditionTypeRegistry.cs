using System;
using AbilityKit.Core.Common.Marker;
using AbilityKit.Samples.Infrastructure.Config.Attributes;

namespace AbilityKit.Samples.Infrastructure.Config
{
    /// <summary>
    /// 行为树条件类型注册表
    /// 通过 BTConditionTypeIdAttribute 自动发现和注册条件实现类型 (HasTargetInRange, NoTarget 等)
    /// </summary>
    public sealed class BTConditionTypeRegistry : KeyedMarkerRegistry<string, BTConditionTypeIdAttribute>
    {
        private static readonly Lazy<BTConditionTypeRegistry> _instance = new(() => new BTConditionTypeRegistry());
        public static BTConditionTypeRegistry Instance => _instance.Value;

        private BTConditionTypeRegistry()
        {
            ScanCurrentAssembly();
        }

        private void ScanCurrentAssembly()
        {
            var assembly = typeof(BTConditionTypeRegistry).Assembly;
            MarkerScanner<BTConditionTypeIdAttribute>.Scan(new[] { assembly }, this);
        }

        internal void RegisterByAttribute(BTConditionTypeIdAttribute attr, Type implType)
        {
            if (attr == null || implType == null) return;
            Register(attr.ConditionName, implType);
        }

        /// <summary>
        /// 根据条件名称创建条件实例
        /// </summary>
        public object CreateCondition(string conditionName)
        {
            return GetOrCreateInstance(conditionName);
        }
    }
}
