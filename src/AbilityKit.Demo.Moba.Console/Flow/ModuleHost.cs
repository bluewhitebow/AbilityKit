using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Console.Flow
{
    /// <summary>
    /// 模块上下文接�?
    /// </summary>
    public interface IModuleContext
    {
    }

    /// <summary>
    /// 模块接口
    /// </summary>
    public interface IGameModule<TContext> where TContext : IModuleContext
    {
        void OnAttach(TContext context);
        void OnDetach(TContext context);
    }

    /// <summary>
    /// Tick 模块接口
    /// </summary>
    public interface IGameModuleTick<TContext> : IGameModule<TContext> where TContext : IModuleContext
    {
        void Tick(TContext context, float deltaTime);
    }

    /// <summary>
    /// Rebind 模块接口
    /// </summary>
    public interface IGameModuleRebind<TContext> : IGameModule<TContext> where TContext : IModuleContext
    {
        void Rebind(TContext context);
    }

    /// <summary>
    /// 模块主机
    /// 管理模块�?Attach/Detach/Tick/Rebind 生命周期
    /// </summary>
    public sealed class ModuleHost<TContext, TModule> : IDisposable where TModule : class, IGameModule<TContext> where TContext : IModuleContext
    {
        private readonly List<TModule> _modules = new();
        private bool _isAttached;

        /// <summary>
        /// 添加模块
        /// </summary>
        public void Add(TModule module)
        {
            if (module == null) return;
            _modules.Add(module);

            if (_isAttached && module is IGameModule<TContext> gm)
            {
                var ctx = GetContext();
                if (ctx != null)
                {
                    gm.OnAttach(ctx);
                }
            }
        }

        /// <summary>
        /// 获取上下文（由子类实现）
        /// </summary>
        protected TContext GetContext() => default;

        /// <summary>
        /// 附加所有模�?
        /// </summary>
        public void Attach(TContext context)
        {
            if (_isAttached) return;
            _isAttached = true;

            foreach (var module in _modules)
            {
                try
                {
                    module.OnAttach(context);
                }
                catch (Exception ex)
                {
                    Platform.Log.Error($"[ModuleHost] OnAttach failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 分离所有模块（反向顺序�?
        /// </summary>
        public void Detach(TContext context)
        {
            if (!_isAttached) return;
            _isAttached = false;

            for (int i = _modules.Count - 1; i >= 0; i--)
            {
                try
                {
                    _modules[i].OnDetach(context);
                }
                catch (Exception ex)
                {
                    Platform.Log.Error($"[ModuleHost] OnDetach failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Tick 所有模�?
        /// </summary>
        public void Tick(TContext context, float deltaTime)
        {
            foreach (var module in _modules)
            {
                if (module is IGameModuleTick<TContext> tickModule)
                {
                    try
                    {
                        tickModule.Tick(context, deltaTime);
                    }
                    catch (Exception ex)
                    {
                        Platform.Log.Error($"[ModuleHost] Tick failed: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Rebind 所有模�?
        /// </summary>
        public void RebindAll(TContext context)
        {
            foreach (var module in _modules)
            {
                if (module is IGameModuleRebind<TContext> rebindModule)
                {
                    try
                    {
                        rebindModule.Rebind(context);
                    }
                    catch (Exception ex)
                    {
                        Platform.Log.Error($"[ModuleHost] Rebind failed: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 获取模块数量
        /// </summary>
        public int ModuleCount => _modules.Count;

        public void Dispose()
        {
            Detach(default);
            _modules.Clear();
        }
    }
}
