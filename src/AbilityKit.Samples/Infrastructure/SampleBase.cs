using System;
using AbilityKit.Samples.Common;
using AbilityKit.Samples.Infrastructure.Config;

namespace AbilityKit.Samples.Infrastructure
{
    /// <summary>
    /// 示例基类
    /// </summary>
    public abstract class SampleBase : ISample
    {
        /// <inheritdoc />
        public abstract string Title { get; }

        /// <inheritdoc />
        public virtual string Description => string.Empty;

        /// <inheritdoc />
        public abstract SampleCategory Category { get; }

        /// <summary>
        /// 日志输出器
        /// </summary>
        protected ILogger Output => AbilityKit.Samples.Common.Logger.Instance;

        /// <summary>
        /// 运行环境（由子类或运行器设置）
        /// </summary>
        protected ISampleEnvironment Environment { get; private set; } = new InstantEnvironment();

        /// <summary>
        /// 配置提供器（可选，由子类设置）
        /// </summary>
        protected IConfigProvider Config { get; private set; }

        /// <summary>
        /// 当前时间
        /// </summary>
        protected float Time => Environment.Time;

        /// <inheritdoc />
        public virtual void Run()
        {
            Section(Title);
            if (!string.IsNullOrEmpty(Description))
            {
                Info(Description);
            }
            Line();

            try
            {
                // 每次运行创建新环境
                Environment = CreateEnvironment();
                Setup();
                OnRun();
            }
            catch (Exception ex)
            {
                Error($"Exception: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建运行环境（可被子类重写）
        /// </summary>
        protected virtual ISampleEnvironment CreateEnvironment()
        {
            return SampleEnvironmentFactory.Create(ExecutionMode.Instant);
        }

        /// <summary>
        /// 设置阶段（运行前）
        /// </summary>
        protected virtual void Setup() { }

        /// <summary>
        /// 子类实现的运行逻辑
        /// </summary>
        protected abstract void OnRun();

        /// <summary>
        /// 记录日志（Info级别）
        /// </summary>
        protected void Log(string message) => Output.Info(message);

        /// <summary>
        /// 信息日志
        /// </summary>
        protected void Info(string message) => Output.Info(message);

        /// <summary>
        /// 显示分节标题
        /// </summary>
        protected void Section(string title) => Output.Section(title);

        /// <summary>
        /// 显示分隔线
        /// </summary>
        protected void Divider() => Output.Divider();

        /// <summary>
        /// 显示空行
        /// </summary>
        protected void Line() => Output.Line();

        /// <summary>
        /// 记录警告
        /// </summary>
        protected void Warn(string message) => Output.Warn(message);

        /// <summary>
        /// 记录错误
        /// </summary>
        protected void Error(string message) => Output.Error(message);

        /// <summary>
        /// 显示项目符号
        /// </summary>
        protected void Bullet(string text) => Output.Bullet(text);

        /// <summary>
        /// 显示编号项
        /// </summary>
        protected void Numbered(int num, string text) => Output.Numbered(num, text);

        /// <summary>
        /// 显示键值对
        /// </summary>
        protected void KeyValue(string key, string value) => Output.KeyValue(key, value);

        /// <summary>
        /// 刷新输出
        /// </summary>
        protected void Flush() => Output.Flush();

        /// <summary>
        /// 推进时间
        /// </summary>
        protected void AdvanceTime(float delta)
        {
            Environment.Advance(delta);
        }

        /// <summary>
        /// 模拟多帧
        /// </summary>
        protected void SimulateFrames(int frames, float deltaPerFrame = 0.016f)
        {
            for (int i = 0; i < frames; i++)
            {
                Environment.Advance(deltaPerFrame);
            }
        }

        /// <summary>
        /// 执行到完成（用于模拟模式）
        /// </summary>
        protected void ExecuteUntilComplete()
        {
            Environment.ExecuteUntilComplete();
        }

        /// <summary>
        /// 推进到指定时间
        /// </summary>
        protected void AdvanceTo(float targetTime)
        {
            Environment.AdvanceTo(targetTime);
        }

        /// <summary>
        /// 暂停
        /// </summary>
        protected void Pause() => Environment.Pause();

        /// <summary>
        /// 继续
        /// </summary>
        protected void Resume() => Environment.Resume();

        /// <summary>
        /// 重置时间
        /// </summary>
        protected void ResetTime() => Environment.Reset();

        #region 配置相关

        /// <summary>
        /// 加载配置文件
        /// </summary>
        protected IConfigProvider LoadConfig(string configName)
        {
            Config = SampleConfigLoader.Instance.Load(configName);
            return Config;
        }

        /// <summary>
        /// 加载配置文件（如果不存在则返回空配置）
        /// </summary>
        protected IConfigProvider LoadConfigOrEmpty(string configName)
        {
            Config = SampleConfigLoader.Instance.LoadOrEmpty(configName);
            return Config;
        }

        /// <summary>
        /// 从内联 JSON 加载配置
        /// </summary>
        protected IConfigProvider LoadConfigFromString(string json, string name = "inline")
        {
            Config = SampleConfigLoader.Instance.LoadFromString(json, name);
            return Config;
        }

        /// <summary>
        /// 获取配置节
        /// </summary>
        protected T GetConfigSection<T>(string sectionName) where T : class, new()
        {
            return Config?.GetSection<T>(sectionName) ?? new T();
        }

        /// <summary>
        /// 获取配置值
        /// </summary>
        protected T GetConfigValue<T>(string key, T defaultValue) where T : class
        {
            return Config?.GetValue(key, defaultValue) ?? defaultValue!;
        }

        /// <summary>
        /// 加载配置文件并获取指定节
        /// </summary>
        protected T LoadConfigSection<T>(string filePath, string sectionName) where T : class, new()
        {
            Config = SampleConfigLoader.Instance.Load(filePath);
            return Config.GetSection<T>(sectionName);
        }

        /// <summary>
        /// 加载配置文件并获取指定节（泛型列表）
        /// </summary>
        protected List<T> LoadConfigSectionList<T>(string filePath, string sectionName)
        {
            Config = SampleConfigLoader.Instance.Load(filePath);
            var section = Config as dynamic;
            return section?.GetSectionOrDefault<List<T>>(sectionName) ?? new List<T>();
        }

        #endregion
    }
}
