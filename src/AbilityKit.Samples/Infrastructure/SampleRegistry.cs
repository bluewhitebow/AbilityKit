using System;
using System.Collections.Generic;
using AbilityKit.Core.Common.Marker;

namespace AbilityKit.Samples.Infrastructure
{
    /// <summary>
    /// 示例注册表
    /// 通过 SampleAttribute 自动发现和注册示例
    /// </summary>
    public sealed class SampleRegistry : IMarkerRegistry
    {
        private static readonly Lazy<SampleRegistry> _instance = new(() => new SampleRegistry());
        public static SampleRegistry Instance => _instance.Value;

        private readonly List<Type> _types = new();
        private readonly Dictionary<Type, ISample> _instances = new();
        private bool _initialized;

        public int Count => _types.Count;
        public IReadOnlyList<Type> Types => _types;

        private SampleRegistry()
        {
        }

        /// <summary>
        /// 初始化并扫描所有示例
        /// </summary>
        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            var assembly = typeof(SampleRegistry).Assembly;
            MarkerScanner<SampleAttribute>.Scan(new[] { assembly }, this);
        }

        internal void RegisterByAttribute(Type implType)
        {
            if (implType == null) return;
            if (implType.IsAbstract) return;
            if (implType.IsInterface) return;
            _types.Add(implType);
        }

        /// <summary>
        /// 注册示例类型
        /// </summary>
        public void Register(Type sampleType)
        {
            if (sampleType == null) return;
            if (sampleType.IsAbstract) return;
            if (sampleType.IsInterface) return;
            if (!_types.Contains(sampleType))
                _types.Add(sampleType);
        }

        /// <summary>
        /// 创建示例实例
        /// </summary>
        public ISample CreateInstance(Type sampleType)
        {
            if (_instances.TryGetValue(sampleType, out var cached))
                return cached;

            var instance = Activator.CreateInstance(sampleType) as ISample;
            if (instance != null)
                _instances[sampleType] = instance;
            return instance;
        }

        /// <summary>
        /// 获取所有示例类型
        /// </summary>
        public IEnumerable<Type> GetAllSampleTypes()
        {
            return _types;
        }

        #region IMarkerRegistry 实现

        public void ForEach(Action<Type> action)
        {
            foreach (var type in _types)
                action(type);
        }

        public IEnumerable<Type> Where(Func<Type, bool> predicate)
        {
            foreach (var type in _types)
                if (predicate(type))
                    yield return type;
        }

        public Type? Find(Func<Type, bool> predicate)
        {
            foreach (var type in _types)
                if (predicate(type))
                    return type;
            return null;
        }

        #endregion
    }
}
