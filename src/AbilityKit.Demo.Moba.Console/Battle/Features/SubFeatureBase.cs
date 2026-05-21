using System;
using System.Collections.Generic;
using System.Linq;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.Battle.Flow;

namespace AbilityKit.Demo.Moba.Console.Battle.Features
{
    /// <summary>
    /// SubFeature 基类
    /// 提供统一的生命周期管理、子模块和 Handler 管理
    ///
    /// 层级结构：
    /// Feature (IFeature)
    /// └── SubFeature (SubFeatureBase) - Level 1
    ///     └── SubSubFeature (SubFeatureBase) - Level 2
    ///         └── Handler (IHandler) - Level 3 (叶子)
    ///
    /// Handler 内部组织：
    /// Handler 内部使用 Phase（顺序）和 Strategy（变体）组织复杂逻辑
    /// </summary>
    public abstract class SubFeatureBase : IFeature, IFeatureTick
    {
        private IFeature? _feature;
        private readonly List<SubFeatureBase> _children = new();
        private readonly List<IHandler> _handlers = new();
        private bool _attached;

        /// <summary>
        /// SubFeature 唯一标识（子类覆盖）
        /// </summary>
        public abstract string Id { get; }

        /// <summary>
        /// 依赖的其他 SubFeature ID 列表
        /// </summary>
        public virtual string[] Dependencies => Array.Empty<string>();

        /// <summary>
        /// 执行优先级，值越小越先执行
        /// </summary>
        public virtual int Priority => 0;

        /// <summary>
        /// Feature 引用（弱引用）
        /// </summary>
        protected IFeature? Feature => _feature;

        /// <summary>
        /// 获取关联的 BattleContext
        /// </summary>
        protected ConsoleBattleContext? BattleContext
        {
            get
            {
                if (_feature is IFeatureContextProvider provider)
                {
                    var ctx = provider.GetContext();
                    if (ctx is FeatureContextAdapter adapter)
                    {
                        return adapter.Context;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// 子模块列表（只读）
        /// </summary>
        public IReadOnlyList<SubFeatureBase> Children => _children;

        /// <summary>
        /// Handler 列表（只读）
        /// </summary>
        public IReadOnlyList<IHandler> Handlers => _handlers;

        /// <summary>
        /// 是否已附加
        /// </summary>
        public bool IsAttached => _attached;

        #region IFeature 实现

        public virtual void OnAttach(IFeatureContext ctx)
        {
            if (_attached)
            {
                Platform.Log.Warn($"[{Id}] Already attached");
                return;
            }

            _attached = true;

            // 内部 OnAttach
            OnAttachInternal(ctx);

            // 按 Priority 排序后递归调用子模块
            foreach (var child in _children.OrderBy(c => c.Priority))
            {
                child.SetFeature(this);
                child.OnAttach(ctx);
            }

            // 按 Order 排序后执行 Handler.OnAttach
            foreach (var handler in _handlers.OrderBy(h => h.Order))
            {
                try
                {
                    handler.OnAttach(ctx);
                }
                catch (Exception ex)
                {
                    Platform.Log.Error($"[{Id}] Handler {handler.GetType().Name} OnAttach failed: {ex.Message}");
                }
            }
        }

        public virtual void OnDetach(IFeatureContext ctx)
        {
            if (!_attached)
            {
                return;
            }

            _attached = false;

            // 反向顺序：先子模块，再 Handler，最后自己
            foreach (var child in _children.AsEnumerable().Reverse())
            {
                child.OnDetach(ctx);
            }

            foreach (var handler in _handlers.AsEnumerable().Reverse())
            {
                try
                {
                    handler.OnDetach(ctx);
                }
                catch (Exception ex)
                {
                    Platform.Log.Error($"[{Id}] Handler {handler.GetType().Name} OnDetach failed: {ex.Message}");
                }
            }

            OnDetachInternal(ctx);
        }

        #endregion

        #region IFeatureTick 实现

        public virtual void Tick(IFeatureContext ctx, float deltaTime)
        {
            if (!_attached)
            {
                return;
            }

            // Tick 子模块
            foreach (var child in _children)
            {
                try
                {
                    child.Tick(ctx, deltaTime);
                }
                catch (Exception ex)
                {
                    Platform.Log.Error($"[{Id}] Child {child.Id} Tick failed: {ex.Message}");
                }
            }

            // 按 Order 排序后执行 Handler.Handle
            foreach (var handler in _handlers.OrderBy(h => h.Order))
            {
                try
                {
                    handler.Handle(ctx, deltaTime);
                }
                catch (Exception ex)
                {
                    Platform.Log.Error($"[{Id}] Handler {handler.GetType().Name} Handle failed: {ex.Message}");
                }
            }
        }

        #endregion

        #region 内部钩子

        /// <summary>
        /// 内部附加钩子，子类可覆盖
        /// </summary>
        protected virtual void OnAttachInternal(IFeatureContext ctx)
        {
        }

        /// <summary>
        /// 内部分离钩子，子类可覆盖
        /// </summary>
        protected virtual void OnDetachInternal(IFeatureContext ctx)
        {
        }

        #endregion

        #region Helper 方法

        /// <summary>
        /// 设置 Feature 引用（内部使用）
        /// </summary>
        internal void SetFeature(IFeature feature)
        {
            _feature = feature;
        }

        /// <summary>
        /// 添加子模块
        /// </summary>
        protected void AddChild(SubFeatureBase child)
        {
            if (child == null) return;
            _children.Add(child);
            _children.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            // 如果已经附加，立即附加子模块
            if (_attached && _feature != null)
            {
                child.SetFeature(this);
                child.OnAttach(_feature is IFeatureContextProvider p ? p.GetContext()! : null!);
            }
        }

        /// <summary>
        /// 移除子模块
        /// </summary>
        protected void RemoveChild(SubFeatureBase child)
        {
            if (child == null) return;

            // 如果已经附加，先分离子模块
            if (_attached)
            {
                if (_feature is IFeatureContextProvider p)
                {
                    child.OnDetach(p.GetContext()!);
                }
            }

            _children.Remove(child);
        }

        /// <summary>
        /// 添加 Handler
        /// </summary>
        protected void AddHandler<T>(T handler) where T : IHandler
        {
            if (handler == null) return;
            _handlers.Add(handler);
            _handlers.Sort((a, b) => a.Order.CompareTo(b.Order));

            // 如果已经附加，立即执行 Handler.OnAttach
            if (_attached && _feature != null)
            {
                if (_feature is IFeatureContextProvider p)
                {
                    handler.OnAttach(p.GetContext()!);
                }
            }
        }

        /// <summary>
        /// 移除 Handler
        /// </summary>
        protected void RemoveHandler(IHandler handler)
        {
            if (handler == null) return;

            // 如果已经附加，先执行 Handler.OnDetach
            if (_attached && _feature != null)
            {
                if (_feature is IFeatureContextProvider p)
                {
                    handler.OnDetach(p.GetContext()!);
                }
            }

            _handlers.Remove(handler);
        }

        /// <summary>
        /// 获取兄弟 SubFeature
        /// </summary>
        protected T? GetSibling<T>() where T : class, IFeature
        {
            if (_feature is IFeatureContainer container)
            {
                return container.GetSubFeature<T>();
            }
            return null;
        }

        /// <summary>
        /// 获取指定 ID 的 SubFeature
        /// </summary>
        protected T? GetSubFeature<T>(string id) where T : class, IFeature
        {
            if (_feature is IFeatureContainer container)
            {
                return container.GetSubFeature<T>(id);
            }
            return null;
        }

        /// <summary>
        /// 获取兄弟 Handler
        /// </summary>
        protected T? GetSiblingHandler<T>() where T : class, IHandler
        {
            // 在同级 Handler 中查找
            foreach (var handler in _handlers)
            {
                if (handler is T result)
                {
                    return result;
                }
            }

            // 在父级 SubFeature 中查找
            if (_feature is IFeatureContainer container)
            {
                return container.GetHandler<T>(Id);
            }

            return null;
        }

        #endregion

        #region Phase/Strategy Helper

        /// <summary>
        /// 按顺序执行多个 Phase
        /// </summary>
        protected void ExecutePhases(IEnumerable<IPhase> phases, IFeatureContext ctx, float deltaTime)
        {
            foreach (var phase in phases.OrderBy(p => p.Order))
            {
                try
                {
                    phase.Execute(ctx, deltaTime);
                }
                catch (Exception ex)
                {
                    Platform.Log.Error($"[{Id}] Phase {phase.GetType().Name} Execute failed: {ex.Message}");
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Feature 容器接口
    /// 提供统一的 SubFeature 和 Handler 访问
    /// </summary>
    public interface IFeatureContainer
    {
        T? GetSubFeature<T>() where T : class, IFeature;
        T? GetSubFeature<T>(string id) where T : class, IFeature;
        T? GetHandler<T>(string subFeatureId) where T : class, IHandler;
        void RegisterSubFeature<T>(T subFeature) where T : IFeature;
    }

    /// <summary>
    /// Feature 上下文提供者接口
    /// </summary>
    public interface IFeatureContextProvider
    {
        IFeatureContext? GetContext();
    }
}
