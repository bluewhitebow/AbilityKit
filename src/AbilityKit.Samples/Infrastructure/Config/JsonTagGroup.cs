namespace AbilityKit.Samples.Infrastructure.Config
{
    /// <summary>
    /// 标签组配置数据模型
    /// </summary>
    public sealed class JsonTagGroup
    {
        /// <summary>
        /// 标签组唯一标识
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 标签组名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 标签组描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 标签列表
        /// </summary>
        public System.Collections.Generic.List<string> Tags { get; set; } = new();
    }
}
