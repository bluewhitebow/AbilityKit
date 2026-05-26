using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Ability.World.DI;

namespace AbilityKit.Demo.Moba.Services.Templates
{
    /// <summary>
    /// 【模板】游戏服务基类
    ///
    /// 所有 Moba Runtime 服务应继承此类。
    /// 提供统一的日志前缀和生命周期管理。
    ///
    /// 使用方法:
    /// 1. 继承 GameServiceBase
    /// 2. 使用 [WorldService] 特性注册
    /// 3. 在构造函数中声明依赖
    ///
    /// 参考文档: Docs/ServiceRegistrationGuide.md
    /// </summary>
    /// <typeparam name="TService">服务类型</typeparam>
    public abstract class GameServiceBase<TService> : IService
        where TService : class
    {
        /// <summary>
        /// 服务名称（用于日志）
        /// </summary>
        protected virtual string ServiceName => typeof(TService).Name;

        /// <summary>
        /// 记录信息日志
        /// </summary>
        protected void LogInfo(string message) => Core.Common.Log.Log.Info($"[{ServiceName}] {message}");

        /// <summary>
        /// 记录警告日志
        /// </summary>
        protected void LogWarning(string message) => Core.Common.Log.Log.Warning($"[{ServiceName}] {message}");

        /// <summary>
        /// 记录错误日志
        /// </summary>
        protected void LogError(string message) => Core.Common.Log.Log.Error($"[{ServiceName}] {message}");

        /// <summary>
        /// 记录异常
        /// </summary>
        protected void LogException(System.Exception ex, string context = "")
        {
            var msg = string.IsNullOrEmpty(context) ? ex.Message : $"{context}: {ex.Message}";
            Core.Common.Log.Log.Exception(ex, $"[{ServiceName}] {msg}");
        }

        public virtual void Dispose()
        {
        }
    }

    /// <summary>
    /// 【模板】需要延迟初始化的服务基类
    ///
    /// 适用于需要在所有服务注册完成后才能初始化的服务。
    /// 实现 IWorldInitializable 接口。
    /// </summary>
    /// <typeparam name="TService">服务类型</typeparam>
    public abstract class GameInitializableServiceBase<TService> : GameServiceBase<TService>, IWorldInitializable
        where TService : class
    {
        /// <summary>
        /// 世界解析器（由框架注入）
        /// </summary>
        protected IWorldResolver? WorldResolver { get; private set; }

        /// <summary>
        /// 尝试解析服务
        /// </summary>
        protected bool TryResolve<T>(out T service) where T : class
        {
            service = null!;
            return WorldResolver?.TryResolve(out service) == true;
        }

        /// <summary>
        /// 解析服务（如果不存在则抛出异常）
        /// </summary>
        protected T Resolve<T>() where T : class
        {
            if (WorldResolver == null)
                throw new System.InvalidOperationException($"[{ServiceName}] WorldResolver is null");
            return WorldResolver.Resolve<T>();
        }

        /// <inheritdoc />
        public virtual void OnInit(IWorldResolver resolver)
        {
            WorldResolver = resolver;
        }
    }

    /// <summary>
    /// 【模板】事件发布者基类
    ///
    /// 提供事件发布的基础实现。
    /// 使用 TriggeringIdUtil.GetEventEid() 转换事件 ID。
    /// </summary>
    /// <typeparam name="TService">服务类型</typeparam>
    public abstract class GameEventPublisherServiceBase<TService> : GameInitializableServiceBase<TService>
        where TService : class
    {
        /// <summary>
        /// 事件总线（由框架注入）
        /// </summary>
        protected AbilityKit.Triggering.Eventing.IEventBus? EventBus { get; private set; }

        /// <inheritdoc />
        public override void OnInit(IWorldResolver resolver)
        {
            base.OnInit(resolver);
            EventBus = resolver.Resolve<AbilityKit.Triggering.Eventing.IEventBus>();
        }

        /// <summary>
        /// 发布事件（双重发布：类型安全 + 对象）
        /// </summary>
        protected void Publish<T>(string eventId, in T payload) where T : struct
        {
            if (EventBus == null)
            {
                LogWarning("EventBus is null, cannot publish event");
                return;
            }

            var eid = TriggeringIdUtil.GetEventEid(eventId);

            // 发布类型安全事件
            EventBus.Publish(new Core.Common.Event.EventKey<T>(eid), in payload);

            // 发布对象事件（通用订阅者）
            object boxed = payload;
            EventBus.Publish(new Core.Common.Event.EventKey<object>(eid), in boxed);
        }
    }
}
