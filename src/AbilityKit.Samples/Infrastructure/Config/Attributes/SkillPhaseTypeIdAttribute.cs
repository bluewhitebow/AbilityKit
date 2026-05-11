using System;

namespace AbilityKit.Samples.Infrastructure.Config.Attributes
{
    /// <summary>
    /// 技能阶段类型标识属性
    /// 用于标记实现了 ISkillPhase 接口的阶段类型 (PreCheck, CastTime, ApplyEffect 等)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class SkillPhaseTypeIdAttribute : AbilityKit.Core.Common.Marker.MarkerAttribute
    {
        public string PhaseName { get; }

        public SkillPhaseTypeIdAttribute(string phaseName)
        {
            PhaseName = phaseName;
        }

        public override void OnScanned(Type implType, AbilityKit.Core.Common.Marker.IMarkerRegistry registry)
        {
            if (registry is SkillPhaseTypeRegistry keyedRegistry)
            {
                keyedRegistry.RegisterByAttribute(this, implType);
            }
        }
    }
}
