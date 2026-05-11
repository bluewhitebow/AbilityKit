using System;
using System.Collections.Generic;

namespace AbilityKit.Samples.Infrastructure.Config
{
    /// <summary>
    /// 示例配置加载器 - 统一管理所有示例配置的加载
    /// 使用 IResourceProvider 抽象资源配置，支持不同平台
    /// </summary>
    public sealed class SampleConfigLoader
    {
        private static SampleConfigLoader _instance;
        private readonly Dictionary<string, JsonConfigProvider> _configs = new();
        private readonly IResourceProvider _resourceProvider;

        public static SampleConfigLoader Instance => _instance ??= new SampleConfigLoader();

        /// <summary>
        /// 使用默认资源提供者初始化
        /// </summary>
        private SampleConfigLoader() : this(ResourceProviders.Current)
        {
        }

        /// <summary>
        /// 使用指定的资源提供者初始化
        /// </summary>
        public SampleConfigLoader(IResourceProvider resourceProvider)
        {
            _resourceProvider = resourceProvider;
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        public JsonConfigProvider Load(string configName)
        {
            if (_configs.TryGetValue(configName, out var existing))
            {
                return existing;
            }

            var content = _resourceProvider.LoadText(configName);
            var provider = JsonConfigProvider.FromString(content);
            _configs[configName] = provider;
            return provider;
        }

        /// <summary>
        /// 加载配置（如果文件不存在则返回空配置）
        /// </summary>
        public JsonConfigProvider LoadOrEmpty(string configName)
        {
            if (_configs.TryGetValue(configName, out var existing))
            {
                return existing;
            }

            if (!_resourceProvider.TryLoadText(configName, out var content))
            {
                var emptyProvider = JsonConfigProvider.FromString("{}");
                _configs[configName] = emptyProvider;
                return emptyProvider;
            }

            var provider = JsonConfigProvider.FromString(content);
            _configs[configName] = provider;
            return provider;
        }

        /// <summary>
        /// 从嵌入资源加载配置
        /// </summary>
        public JsonConfigProvider LoadFromString(string json, string name = "inline")
        {
            if (_configs.TryGetValue(name, out var existing))
            {
                return existing;
            }

            var provider = JsonConfigProvider.FromString(json);
            _configs[name] = provider;
            return provider;
        }

        /// <summary>
        /// 卸载配置
        /// </summary>
        public void Unload(string configName)
        {
            if (_configs.TryGetValue(configName, out var provider))
            {
                provider.Dispose();
                _configs.Remove(configName);
            }
        }

        /// <summary>
        /// 卸载所有配置
        /// </summary>
        public void UnloadAll()
        {
            foreach (var kvp in _configs)
            {
                kvp.Value.Dispose();
            }
            _configs.Clear();
        }
    }
}
