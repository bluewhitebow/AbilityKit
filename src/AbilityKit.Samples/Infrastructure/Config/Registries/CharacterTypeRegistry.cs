using System;
using AbilityKit.Core.Common.Marker;
using AbilityKit.Samples.Infrastructure.Config.Attributes;

namespace AbilityKit.Samples.Infrastructure.Config
{
    /// <summary>
    /// 角色类型注册表
    /// 通过 CharacterTypeIdAttribute 自动发现和注册角色实现类型 (Hero, Boss, Tower 等)
    /// </summary>
    public sealed class CharacterTypeRegistry : KeyedMarkerRegistry<string, CharacterTypeIdAttribute>
    {
        private static readonly Lazy<CharacterTypeRegistry> _instance = new(() => new CharacterTypeRegistry());
        public static CharacterTypeRegistry Instance => _instance.Value;

        private CharacterTypeRegistry()
        {
            ScanCurrentAssembly();
        }

        private void ScanCurrentAssembly()
        {
            var assembly = typeof(CharacterTypeRegistry).Assembly;
            MarkerScanner<CharacterTypeIdAttribute>.Scan(new[] { assembly }, this);
        }

        internal void RegisterByAttribute(CharacterTypeIdAttribute attr, Type implType)
        {
            if (attr == null || implType == null) return;
            Register(attr.CharacterId, implType);
        }

        /// <summary>
        /// 根据角色 ID 创建角色实例
        /// </summary>
        public object CreateCharacter(string characterId)
        {
            return GetOrCreateInstance(characterId);
        }
    }
}
