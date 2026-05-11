namespace AbilityKit.Samples.Infrastructure.Config
{
    /// <summary>
    /// 配置文件路径常量
    /// </summary>
    public static class ConfigPaths
    {
        /// <summary>
        /// 管线配置文件
        /// </summary>
        public const string PipelineConfig = "Configs/PipelineConfig.json";

        /// <summary>
        /// 示例配置
        /// </summary>
        public const string SampleConfigs = "Configs/SampleConfigs.json";

        /// <summary>
        /// 标签配置文件
        /// </summary>
        public const string TagsConfig = "Configs/TagsConfig.json";
    }

    /// <summary>
    /// JSON 配置节名称常量
    /// </summary>
    public static class ConfigSections
    {
        /// <summary>
        /// 管线配置节
        /// </summary>
        public const string PipelineConfigs = "pipelineConfigs";

        /// <summary>
        /// 角色标签配置节
        /// </summary>
        public const string CharacterTags = "characterTags";

        /// <summary>
        /// 标签组配置节
        /// </summary>
        public const string TagGroups = "tagGroups";
    }
}
