using System;

namespace AbilityKit.Samples.Infrastructure.Config.Attributes
{
    /// <summary>
    /// 角色标签标识属性
    /// 用于标记角色所属的标签类别 (Hero, Boss, Tank, Healer 等)
    /// 一个角色可以有多个标签
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class CharacterTagAttribute : AbilityKit.Core.Common.Marker.MarkerAttribute
    {
        public string Tag { get; }

        public CharacterTagAttribute(string tag)
        {
            Tag = tag;
        }

        public override void OnScanned(Type implType, AbilityKit.Core.Common.Marker.IMarkerRegistry registry)
        {
            if (registry is CharacterTagRegistry keyedRegistry)
            {
                keyedRegistry.RegisterByAttribute(this, implType);
            }
        }
    }
}
