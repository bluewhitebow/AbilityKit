using System;

namespace AbilityKit.Samples.Infrastructure
{
    /// <summary>
    /// 示例标记属性
    /// 标记在 SampleBase 子类上，自动注册到 SampleRunner
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class SampleAttribute : AbilityKit.Core.Common.Marker.MarkerAttribute
    {
        public override void OnScanned(Type implType, AbilityKit.Core.Common.Marker.IMarkerRegistry registry)
        {
            if (registry is SampleRegistry sampleRegistry)
            {
                sampleRegistry.RegisterByAttribute(implType);
            }
        }
    }
}
