using System.Collections.Generic;
using AbilityKit.GameplayTags;

namespace AbilityKit.Samples.Infrastructure.Config
{
    /// <summary>
    /// 强类型配置访问器
    /// </summary>
    public static class SampleConfig
    {
        private static readonly SampleConfigLoader _loader = SampleConfigLoader.Instance;

        /// <summary>
        /// 加载管线配置
        /// </summary>
        public static List<T> LoadPipelines<T>() where T : class
        {
            var provider = _loader.Load(ConfigPaths.PipelineConfig);
            var section = provider as dynamic;
            return section?.GetSectionOrDefault<List<T>>("pipelineConfigs") ?? new List<T>();
        }

        /// <summary>
        /// 加载标签组配置
        /// </summary>
        public static List<TagGroupConfig> LoadTagGroups()
        {
            var provider = _loader.Load(ConfigPaths.TagsConfig);
            return provider.GetSection<List<TagGroupConfig>>(ConfigSections.TagGroups);
        }

        /// <summary>
        /// 加载标签组并注册到 GameplayTagManager
        /// </summary>
        public static List<GameplayTag> LoadAndRegisterTags()
        {
            var groups = LoadTagGroups();
            var registeredTags = new List<GameplayTag>();

            foreach (var group in groups)
            {
                foreach (var tagName in group.Tags)
                {
                    var tag = GameplayTagManager.Instance.RequestTag(tagName);
                    registeredTags.Add(tag);
                }
            }

            return registeredTags;
        }

        /// <summary>
        /// 加载角色标签配置
        /// </summary>
        public static List<T> LoadCharacterTags<T>() where T : class
        {
            var provider = _loader.Load(ConfigPaths.SampleConfigs);
            var section = provider as dynamic;
            return section?.GetSectionOrDefault<List<T>>(ConfigSections.CharacterTags) ?? new List<T>();
        }

        /// <summary>
        /// 通用配置加载方法
        /// </summary>
        public static List<T> Load<T>(string filePath, string sectionName) where T : class
        {
            var provider = _loader.Load(filePath);
            var section = provider as dynamic;
            return section?.GetSectionOrDefault<List<T>>(sectionName) ?? new List<T>();
        }
    }
}
