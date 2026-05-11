using System;
using System.Collections.Generic;
using AbilityKit.Core.Common.Pool;
using AbilityKit.Samples.Infrastructure.Config.Attributes;

namespace AbilityKit.Samples.Samples.Pipeline
{
    /// <summary>
    /// 阶段执行器注册表
    /// 使用对象池管理执行器实例的复用
    /// 通过 ExecutorForAttribute 自动发现配置类型与执行器的映射
    /// </summary>
    public sealed class PhaseExecutorRegistry
    {
        private static readonly Lazy<PhaseExecutorRegistry> _instance = new(() => new PhaseExecutorRegistry());
        public static PhaseExecutorRegistry Instance => _instance.Value;

        private readonly Dictionary<Type, ObjectPool<IPhaseExecutor>> _pools = new();
        private readonly Dictionary<Type, int> _poolMaxSizes = new();
        private readonly ExecutorForRegistry _executorForRegistry = ExecutorForRegistry.Instance;

        private PhaseExecutorRegistry()
        {
            RegisterBuiltins();
        }

        private void RegisterBuiltins()
        {
            RegisterPooled<PreCheckExecutor>(maxSize: 10);
            RegisterPooled<ValidationExecutor>(maxSize: 10);
            RegisterPooled<CastingExecutor>(maxSize: 5);
            RegisterPooled<ExecuteExecutor>(maxSize: 10);
            RegisterPooled<CooldownExecutor>(maxSize: 10);
        }

        /// <summary>
        /// 注册带对象池的执行器
        /// </summary>
        public void RegisterPooled<TExecutor>(int maxSize = 10) where TExecutor : class, IPhaseExecutor, new()
        {
            var options = new ObjectPoolOptions<IPhaseExecutor>(() => new TExecutor())
            {
                MaxSize = maxSize,
                DefaultCapacity = 2,
                OnRelease = obj => obj.OnPoolRelease(),
                OnGet = obj => obj.OnPoolGet()
            };

            var pool = new ObjectPool<IPhaseExecutor>(options);
            _pools[typeof(TExecutor)] = pool;
            _poolMaxSizes[typeof(TExecutor)] = maxSize;
        }

        /// <summary>
        /// 获取执行器实例（从池中获取）
        /// </summary>
        public IPhaseExecutor Rent<TExecutor>() where TExecutor : class, IPhaseExecutor, new()
        {
            if (_pools.TryGetValue(typeof(TExecutor), out var pool))
            {
                return pool.Get();
            }
            return new TExecutor();
        }

        /// <summary>
        /// 归还执行器实例（归还到池中）
        /// </summary>
        public void Return(IPhaseExecutor executor)
        {
            if (executor == null) return;

            if (_pools.TryGetValue(executor.GetType(), out var pool))
            {
                pool.Release(executor);
            }
        }

        /// <summary>
        /// 根据配置类型获取对应的执行器（从池中获取）
        /// </summary>
        public IPhaseExecutor Rent(Type executorType)
        {
            if (executorType == null) return null;

            if (_pools.TryGetValue(executorType, out var pool))
            {
                return pool.Get();
            }

            // 如果没有池，直接创建
            return Activator.CreateInstance(executorType) as IPhaseExecutor;
        }

        /// <summary>
        /// 根据配置获取执行器（自动推断类型）
        /// </summary>
        public IPhaseExecutor Rent(object config)
        {
            if (config == null) return null;

            var configType = config.GetType();
            var executorType = _executorForRegistry.GetExecutorType(configType);
            if (executorType != null)
            {
                return Rent(executorType);
            }

            return null;
        }

        /// <summary>
        /// 根据配置类型获取对应的执行器类型
        /// </summary>
        public Type GetExecutorType(Type configType)
        {
            return _executorForRegistry.GetExecutorType(configType);
        }

        /// <summary>
        /// 预热池
        /// </summary>
        public void Prewarm(int count = 5)
        {
            foreach (var pool in _pools.Values)
            {
                pool.Prewarm(count);
            }
        }

        /// <summary>
        /// 获取所有已注册的执行器信息
        /// </summary>
        public IEnumerable<(string ExecutorName, int PoolSize, int InactiveCount)> GetPoolStats()
        {
            foreach (var kvp in _pools)
            {
                var maxSize = _poolMaxSizes.GetValueOrDefault(kvp.Key, 0);
                yield return (kvp.Key.Name, maxSize, kvp.Value.InactiveCount);
            }
        }

        /// <summary>
        /// 获取所有配置→执行器的映射
        /// </summary>
        public IEnumerable<(Type ConfigType, Type ExecutorType)> GetAllMappings()
        {
            return _executorForRegistry.GetAllMappings();
        }
    }

    /// <summary>
    /// 池化执行器的包装器
    /// 析构时自动归还到池
    /// </summary>
    public sealed class PooledExecutor : IDisposable
    {
        private readonly PhaseExecutorRegistry _registry;
        public IPhaseExecutor Executor { get; }

        internal PooledExecutor(IPhaseExecutor executor, PhaseExecutorRegistry registry)
        {
            Executor = executor;
            _registry = registry;
        }

        public void Dispose()
        {
            _registry?.Return(Executor);
        }
    }

    /// <summary>
    /// 执行器上下文扩展
    /// </summary>
    public static class PhaseExecutorContextExtensions
    {
        /// <summary>
        /// 租赁执行器（使用池）
        /// </summary>
        public static PooledExecutor RentExecutor(this PhaseExecutorRegistry registry, object config)
        {
            var executor = registry.Rent(config);
            return new PooledExecutor(executor, registry);
        }
    }
}
