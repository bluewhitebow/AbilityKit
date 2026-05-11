using System;

namespace AbilityKit.Samples.Infrastructure.Config.Attributes
{
    /// <summary>
    /// 角色类型标识属性
    /// 用于标记实现了 ICharacter 接口的角色实现类 (Hero, Boss, Tower 等)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class CharacterTypeIdAttribute : AbilityKit.Core.Common.Marker.MarkerAttribute
    {
        public string CharacterId { get; }

        public CharacterTypeIdAttribute(string characterId)
        {
            CharacterId = characterId;
        }

        public override void OnScanned(Type implType, AbilityKit.Core.Common.Marker.IMarkerRegistry registry)
        {
            if (registry is CharacterTypeRegistry keyedRegistry)
            {
                keyedRegistry.RegisterByAttribute(this, implType);
            }
        }
    }
}
