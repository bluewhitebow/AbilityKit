namespace AbilityKit.Samples.Infrastructure.Config
{
    /// <summary>
    /// 标签组配置
    /// </summary>
    public class TagGroupConfig
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string[] Tags { get; set; }
    }
}
