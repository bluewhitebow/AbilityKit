using System;

namespace AbilityKit.Samples.Infrastructure.Config
{
    /// <summary>
    /// 资源配置提供者全局访问器
    /// 通过依赖注入/Setter注入来切换不同的 IResourceProvider 实现
    /// </summary>
    public static class ResourceProviders
    {
        private static IResourceProvider _current;

        /// <summary>
        /// 当前资源配置提供者
        /// </summary>
        public static IResourceProvider Current
        {
            get => _current ??= CreateDefault();
            set => _current = value;
        }

        /// <summary>
        /// 创建默认的资源提供者
        /// </summary>
        public static IResourceProvider CreateDefault()
        {
            // 在运行时自动检测环境
            // TODO: 后续可以通过环境变量或启动参数切换
            return new FileSystemResourceProvider();
        }

        /// <summary>
        /// 设置资源提供者
        /// </summary>
        /// <typeparam name="T">资源提供者类型</typeparam>
        public static void Set<T>() where T : IResourceProvider, new()
        {
            _current = new T();
        }

        /// <summary>
        /// 重置为默认资源提供者
        /// </summary>
        public static void Reset()
        {
            _current = CreateDefault();
        }
    }
}
