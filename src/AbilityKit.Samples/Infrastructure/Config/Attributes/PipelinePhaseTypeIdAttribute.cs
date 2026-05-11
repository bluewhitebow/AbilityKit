using System;

namespace AbilityKit.Samples.Infrastructure.Config.Attributes
{
    /// <summary>
    /// 管线阶段类型标识属性
    /// 用于标记实现了 IPipelinePhase 接口的阶段类型
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class PipelinePhaseTypeIdAttribute : AbilityKit.Core.Common.Marker.MarkerAttribute
    {
        public string TypeName { get; }
        public bool IsTimed { get; }

        public PipelinePhaseTypeIdAttribute(string typeName, bool isTimed = false)
        {
            TypeName = typeName;
            IsTimed = isTimed;
        }

        public override void OnScanned(Type implType, AbilityKit.Core.Common.Marker.IMarkerRegistry registry)
        {
            if (registry is PipelinePhaseRegistry keyedRegistry)
            {
                keyedRegistry.RegisterByAttribute(this, implType);
            }
        }
    }
}
