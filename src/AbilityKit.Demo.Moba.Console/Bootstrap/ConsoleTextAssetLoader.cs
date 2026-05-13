using System;
using System.IO;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;

namespace AbilityKit.Demo.Moba.Console.Bootstrap
{
    /// <summary>
    /// Console 环境实现的 TextAsset 加载器。
    /// 从文件系统加载配置，支持 JSON 等文本资源。
    /// 通过 [WorldService] 属性自动注册到 World。
    /// </summary>
    [WorldService(typeof(ITextAssetLoader), WorldLifetime.Singleton)]
    public sealed class ConsoleTextAssetLoader : ITextAssetLoader
    {
        private readonly string _basePath;

        public ConsoleTextAssetLoader() : this(GetDefaultBasePath())
        {
        }

        public ConsoleTextAssetLoader(string basePath)
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
                Platform.Log.Debug($"[ConsoleTextAssetLoader] File not found: {fullPath}");
                return false;
            }

            try
            {
                text = File.ReadAllText(fullPath);
                return !string.IsNullOrEmpty(text);
            }
            catch (Exception ex)
            {
                Platform.Log.Warn($"[ConsoleTextAssetLoader] Failed to read file: {fullPath}, Error: {ex.Message}");
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
                Platform.Log.Debug($"[ConsoleTextAssetLoader] File not found: {fullPath}");
                return false;
            }

            try
            {
                bytes = File.ReadAllBytes(fullPath);
                return bytes != null && bytes.Length > 0;
            }
            catch (Exception ex)
            {
                Platform.Log.Warn($"[ConsoleTextAssetLoader] Failed to read bytes: {fullPath}, Error: {ex.Message}");
                return false;
            }
        }

        private string GetFullPath(string path)
        {
            if (Path.IsPathRooted(path))
            {
                return path;
            }
            return Path.Combine(_basePath, path);
        }

        private static string GetDefaultBasePath()
        {
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var configDir = Path.Combine(exeDir, "Configs");
            if (Directory.Exists(configDir))
            {
                return configDir;
            }

            var projectRoot = Path.GetFullPath(Path.Combine(exeDir, "..", "..", "..", ".."));
            return Path.Combine(projectRoot, "Configs");
        }
    }
}
