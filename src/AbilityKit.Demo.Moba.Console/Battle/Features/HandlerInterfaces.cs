using AbilityKit.Demo.Moba.Console.Battle.Flow;

namespace AbilityKit.Demo.Moba.Console.Battle.Features
{
    /// <summary>
    /// Handler 基础接口
    /// 最小执行单元，负责具体业务逻辑
    /// </summary>
    public interface IHandler
    {
        /// <summary>
        /// Handler 执行顺序，值越小越先执行
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Handler 附加到上下文
        /// </summary>
        void OnAttach(IFeatureContext ctx);

        /// <summary>
        /// Handler 从上下文分离
        /// </summary>
        void OnDetach(IFeatureContext ctx);

        /// <summary>
        /// 处理逻辑
        /// </summary>
        void Handle(IFeatureContext ctx, float deltaTime);
    }

    /// <summary>
    /// Handler 基类
    /// 提供默认实现，简化 Handler 创建
    /// </summary>
    public abstract class HandlerBase : IHandler
    {
        public virtual int Order => 0;

        public virtual void OnAttach(IFeatureContext ctx)
        {
        }

        public virtual void OnDetach(IFeatureContext ctx)
        {
        }

        public abstract void Handle(IFeatureContext ctx, float deltaTime);

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
                    Platform.Log.Error($"[{GetType().Name}] Phase {phase.GetType().Name} Execute failed: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Phase 基础接口
    /// 用于 Handler 内部有序处理阶段
    /// </summary>
    public interface IPhase
    {
        /// <summary>
        /// Phase 执行顺序
        /// </summary>
        int Order { get; }

        /// <summary>
        /// 执行阶段
        /// </summary>
        void Execute(IFeatureContext ctx, float deltaTime);
    }

    /// <summary>
    /// Phase 基类
    /// </summary>
    public abstract class PhaseBase : IPhase
    {
        public virtual int Order => 0;

        public abstract void Execute(IFeatureContext ctx, float deltaTime);
    }

    /// <summary>
    /// Strategy 基础接口
    /// 用于 Handler 内部可变算法
    /// </summary>
    public interface IStrategy
    {
        /// <summary>
        /// 执行策略
        /// </summary>
        void Execute(IFeatureContext ctx, float deltaTime);
    }

    /// <summary>
    /// Strategy 基类
    /// </summary>
    public abstract class StrategyBase : IStrategy
    {
        public abstract void Execute(IFeatureContext ctx, float deltaTime);
    }
}
