using System;
using System.IO;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;

namespace ET.Logic
{
    /// <summary>
    /// ET 环境实现�?TextAsset 加载�?
    /// 从文件系统加载配置，支持 JSON 等文本资�?
    /// </summary>
    [WorldService(typeof(ITextAssetLoader), WorldLifetime.Singleton)]
    public sealed class ETTextAssetLoader : ITextAssetLoader
    {
        private readonly string _basePath;

        public ETTextAssetLoader() : this(GetDefaultBasePath())
        {
        }

        public ETTextAssetLoader(string basePath)
        {
            _basePath = string.IsNullOrEmpty(basePath) ? GetDefaultBasePath() : basePath;
        }

        public bool TryLoadText(string path, out string text)
        {
            text = null;
            if (string.IsNullOrEmpty(path)) return false;

            var fullPath = GetFullPath(path);

            if (!File.Exists(fullPath))
            {
                return false;
            }

            try
            {
                text = File.ReadAllText(fullPath);
                return !string.IsNullOrEmpty(text);
            }
            catch
            {
                return false;
            }
        }

        public bool TryLoadBytes(string path, out byte[] bytes)
        {
            bytes = null;
            if (string.IsNullOrEmpty(path)) return false;

            var fullPath = GetFullPath(path);

            if (!File.Exists(fullPath))
            {
                return false;
            }

            try
            {
                bytes = File.ReadAllBytes(fullPath);
                return bytes != null && bytes.Length > 0;
            }
            catch (Exception ex)
            {
                Log.Warning($"[ETTextAssetLoader] Failed to read bytes: {fullPath}, Error: {ex.Message}");
                return false;
            }
        }

        private string GetFullPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return _basePath;

            var normalizedPath = path.Replace('/', Path.DirectorySeparatorChar);

            if (Path.IsPathRooted(normalizedPath))
            {
                return normalizedPath;
            }
            return Path.Combine(_basePath, normalizedPath);
        }

        private static string GetDefaultBasePath()
        {
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var configDir = Path.Combine(exeDir, "Configs");

            if (Directory.Exists(configDir))
            {
                return configDir;
            }

            // 尝试向上查找项目根目�?
            var current = new DirectoryInfo(exeDir);
            while (current != null)
            {
                var testPath = Path.Combine(current.FullName, "Configs");
                if (Directory.Exists(testPath))
                {
                    return testPath;
                }
                current = current.Parent;
            }

            // 默认返回 exe 目录
            return exeDir;
        }
    }
}
