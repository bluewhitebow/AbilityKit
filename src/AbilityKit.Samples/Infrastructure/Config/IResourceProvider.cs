using System;

namespace AbilityKit.Samples.Infrastructure.Config
{
    /// <summary>
    /// 资源加载器接口
    /// 抽象资源配置加载，支持不同平台（文件系统、Unity Resources、Addressables等）
    /// </summary>
    public interface IResourceProvider
    {
        /// <summary>
        /// 加载文本资源
        /// </summary>
        string LoadText(string path);

        /// <summary>
        /// 尝试加载文本资源
        /// </summary>
        bool TryLoadText(string path, out string content);

        /// <summary>
        /// 检查资源是否存在
        /// </summary>
        bool Exists(string path);

        /// <summary>
        /// 获取资源路径（标准化）
        /// </summary>
        string NormalizePath(string path);
    }
}
