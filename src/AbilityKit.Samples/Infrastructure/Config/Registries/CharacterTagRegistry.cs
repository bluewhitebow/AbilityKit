using System;
using System.Collections.Generic;
using AbilityKit.Core.Common.Marker;
using AbilityKit.Samples.Infrastructure.Config.Attributes;

namespace AbilityKit.Samples.Infrastructure.Config
{
    /// <summary>
    /// 角色标签注册表
    /// 通过 CharacterTagAttribute 自动发现和注册角色标签
    /// 一个角色可以有多个标签
    /// </summary>
    public sealed class CharacterTagRegistry : IMarkerRegistry
    {
        private static readonly Lazy<CharacterTagRegistry> _instance = new(() => new CharacterTagRegistry());
        public static CharacterTagRegistry Instance => _instance.Value;

        private readonly Dictionary<string, List<Type>> _tagToTypes = new();
        private readonly List<Type> _allTypes = new();

        private CharacterTagRegistry()
        {
            ScanCurrentAssembly();
        }

        private void ScanCurrentAssembly()
        {
            var assembly = typeof(CharacterTagRegistry).Assembly;
            MarkerScanner<CharacterTagAttribute>.Scan(new[] { assembly }, this);
        }

        internal void RegisterByAttribute(CharacterTagAttribute attr, Type implType)
        {
            if (attr == null || implType == null) return;

            if (!_tagToTypes.ContainsKey(attr.Tag))
                _tagToTypes[attr.Tag] = new List<Type>();

            if (!_tagToTypes[attr.Tag].Contains(implType))
                _tagToTypes[attr.Tag].Add(implType);

            if (!_allTypes.Contains(implType))
                _allTypes.Add(implType);
        }

        /// <summary>
        /// 根据标签查找所有具有该标签的角色类型
        /// </summary>
        public IEnumerable<Type> GetTypesByTag(string tag)
        {
            return _tagToTypes.TryGetValue(tag, out var types) ? types : Enumerable.Empty<Type>();
        }

        /// <summary>
        /// 检查某个类型是否具有指定标签
        /// </summary>
        public bool HasTag(Type type, string tag)
        {
            return _tagToTypes.TryGetValue(tag, out var types) && types.Contains(type);
        }

        /// <summary>
        /// 获取所有已注册的标签
        /// </summary>
        public IEnumerable<string> AllTags => _tagToTypes.Keys;

        #region IMarkerRegistry 实现

        public int Count => _allTypes.Count;
        public IReadOnlyList<Type> Types => _allTypes;

        public void Register(Type implType)
        {
            if (implType == null) return;
            if (implType.IsAbstract) return;
            if (implType.IsInterface) return;
            if (!_allTypes.Contains(implType))
                _allTypes.Add(implType);
        }

        public void ForEach(Action<Type> action)
        {
            foreach (var type in _allTypes)
                action(type);
        }

        public IEnumerable<Type> Where(Func<Type, bool> predicate)
        {
            foreach (var type in _allTypes)
                if (predicate(type))
                    yield return type;
        }

        public Type? Find(Func<Type, bool> predicate)
        {
            foreach (var type in _allTypes)
                if (predicate(type))
                    return type;
            return null;
        }

        #endregion
    }
}
