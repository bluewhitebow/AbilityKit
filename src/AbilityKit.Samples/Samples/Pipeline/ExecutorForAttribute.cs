using System;
using System.Linq;
using AbilityKit.Core.Common.Marker;

namespace AbilityKit.Samples.Samples.Pipeline
{
    /// <summary>
    /// 标记配置类型对应的执行器
    /// 使用此 Attribute 标记配置类，自动建立映射
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ExecutorForAttribute : MarkerAttribute
    {
        public Type ExecutorType { get; }

        public ExecutorForAttribute(Type executorType)
        {
            ExecutorType = executorType;
        }

        public override void OnScanned(Type implType, IMarkerRegistry registry)
        {
            if (registry is ExecutorForRegistry typedRegistry && ExecutorType != null)
            {
                typedRegistry.Register(implType, ExecutorType);
            }
        }
    }

    /// <summary>
    /// 配置→执行器映射注册表
    /// Key: 配置类型, Value: 执行器类型
    /// </summary>
    public sealed class ExecutorForRegistry : KeyedMarkerRegistry<Type, ExecutorForAttribute>
    {
        public static ExecutorForRegistry Instance { get; } = new();

        private ExecutorForRegistry()
        {
            ScanCurrentAssembly();
        }

        private void ScanCurrentAssembly()
        {
            var assembly = typeof(ExecutorForRegistry).Assembly;
            MarkerScanner<ExecutorForAttribute>.Scan(new[] { assembly }, this);
        }

        /// <summary>
        /// 根据配置类型获取对应的执行器类型
        /// </summary>
        public Type GetExecutorType(Type configType)
        {
            if (TryGet(configType, out var executorType))
            {
                return executorType;
            }

            // 尝试查找基类
            var baseType = configType.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                if (TryGet(baseType, out executorType))
                {
                    return executorType;
                }
                baseType = baseType.BaseType;
            }

            return null;
        }

        /// <summary>
        /// 获取所有配置→执行器的映射
        /// </summary>
        public IEnumerable<(Type ConfigType, Type ExecutorType)> GetAllMappings()
        {
            return Keys.Select(key => (key, TryGet(key, out var executorType) ? executorType : null))
                       .Where(tuple => tuple.Item2 != null)
                       .Select(tuple => (tuple.key, tuple.Item2!));
        }
    }
}
