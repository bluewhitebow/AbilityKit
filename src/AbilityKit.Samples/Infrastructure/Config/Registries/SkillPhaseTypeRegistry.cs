using System;
using AbilityKit.Core.Common.Marker;
using AbilityKit.Samples.Infrastructure.Config.Attributes;

namespace AbilityKit.Samples.Infrastructure.Config
{
    /// <summary>
    /// 技能阶段类型注册表
    /// 通过 SkillPhaseTypeIdAttribute 自动发现和注册技能阶段类型 (PreCheck, CastTime, ApplyEffect 等)
    /// </summary>
    public sealed class SkillPhaseTypeRegistry : KeyedMarkerRegistry<string, SkillPhaseTypeIdAttribute>
    {
        private static readonly Lazy<SkillPhaseTypeRegistry> _instance = new(() => new SkillPhaseTypeRegistry());
        public static SkillPhaseTypeRegistry Instance => _instance.Value;

        private SkillPhaseTypeRegistry()
        {
            ScanCurrentAssembly();
        }

        private void ScanCurrentAssembly()
        {
            var assembly = typeof(SkillPhaseTypeRegistry).Assembly;
            MarkerScanner<SkillPhaseTypeIdAttribute>.Scan(new[] { assembly }, this);
        }

        internal void RegisterByAttribute(SkillPhaseTypeIdAttribute attr, Type implType)
        {
            if (attr == null || implType == null) return;
            Register(attr.PhaseName, implType);
        }

        /// <summary>
        /// 根据阶段名称创建技能阶段实例
        /// </summary>
        public object CreatePhase(string phaseName)
        {
            return GetOrCreateInstance(phaseName);
        }
    }
}
