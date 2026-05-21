using System.Collections.Generic;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.Battle.Flow;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.Battle.Features
{
    /// <summary>
    /// 帧同步 Handler 示例
    /// 演示如何使用 Handler + Phase + Strategy 架构
    /// </summary>
    public sealed class FrameSyncHandler : HandlerBase
    {
        public override int Order => 100;

        private readonly List<IPhase> _phases = new();
        private IFrameSyncStrategy _strategy;
        private int _syncCounter;

        public FrameSyncHandler()
        {
            _strategy = new ClientFrameSyncStrategy();

            // 注册处理阶段
            _phases.Add(new CollectInputPhase());
            _phases.Add(new SimulatePhase());
            _phases.Add(new StoreSnapshotPhase());
            _phases.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        public override void OnAttach(IFeatureContext ctx)
        {
            Platform.Log.System("[FrameSyncHandler] Attached");
        }

        public override void OnDetach(IFeatureContext ctx)
        {
            Platform.Log.System("[FrameSyncHandler] Detached");
        }

        public override void Handle(IFeatureContext ctx, float deltaTime)
        {
            ExecutePhases(_phases, ctx, deltaTime);
        }

        /// <summary>
        /// 设置帧同步策略
        /// </summary>
        public void SetStrategy(IFrameSyncStrategy strategy)
        {
            _strategy = strategy ?? new ClientFrameSyncStrategy();
        }

        #region Phase 实现

        private class CollectInputPhase : PhaseBase
        {
            public override int Order => 1;

            public override void Execute(IFeatureContext ctx, float deltaTime)
            {
                // 采集输入
            }
        }

        private class SimulatePhase : PhaseBase
        {
            public override int Order => 2;

            public override void Execute(IFeatureContext ctx, float deltaTime)
            {
                // 模拟
            }
        }

        private class StoreSnapshotPhase : PhaseBase
        {
            public override int Order => 3;

            public override void Execute(IFeatureContext ctx, float deltaTime)
            {
                // 存储快照
            }
        }

        #endregion
    }

    /// <summary>
    /// 帧同步策略接口
    /// </summary>
    public interface IFrameSyncStrategy
    {
        void Sync(IFeatureContext ctx, float deltaTime);
    }

    /// <summary>
    /// 客户端帧同步策略
    /// </summary>
    public sealed class ClientFrameSyncStrategy : IFrameSyncStrategy
    {
        public void Sync(IFeatureContext ctx, float deltaTime)
        {
            // 客户端帧同步逻辑
        }
    }

    /// <summary>
    /// 服务端帧同步策略
    /// </summary>
    public sealed class ServerFrameSyncStrategy : IFrameSyncStrategy
    {
        public void Sync(IFeatureContext ctx, float deltaTime)
        {
            // 服务端帧同步逻辑
        }
    }

    /// <summary>
    /// 预测 Handler 示例
    /// 演示如何组织复杂的预测逻辑
    /// </summary>
    public sealed class PredictionHandler : HandlerBase
    {
        public override int Order => 200;

        private readonly List<IPhase> _phases = new();
        private IPredictionStrategy _strategy;

        public PredictionHandler()
        {
            _strategy = new LinearPredictionStrategy();

            _phases.Add(new CollectPhase());
            _phases.Add(new PredictPhase());
            _phases.Add(new StorePhase());
            _phases.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        public override void OnAttach(IFeatureContext ctx)
        {
            Platform.Log.View("[PredictionHandler] Attached");
        }

        public override void OnDetach(IFeatureContext ctx)
        {
            Platform.Log.View("[PredictionHandler] Detached");
        }

        public override void Handle(IFeatureContext ctx, float deltaTime)
        {
            ExecutePhases(_phases, ctx, deltaTime);
        }

        /// <summary>
        /// 设置预测策略
        /// </summary>
        public void SetStrategy(IPredictionStrategy strategy)
        {
            _strategy = strategy ?? new LinearPredictionStrategy();
        }

        #region Phase 实现

        private class CollectPhase : PhaseBase
        {
            public override int Order => 1;

            public override void Execute(IFeatureContext ctx, float deltaTime)
            {
                // 采集当前状态
            }
        }

        private class PredictPhase : PhaseBase
        {
            public override int Order => 2;

            public override void Execute(IFeatureContext ctx, float deltaTime)
            {
                // 执行预测
            }
        }

        private class StorePhase : PhaseBase
        {
            public override int Order => 3;

            public override void Execute(IFeatureContext ctx, float deltaTime)
            {
                // 存储预测结果
            }
        }

        #endregion
    }

    /// <summary>
    /// 预测策略接口
    /// </summary>
    public interface IPredictionStrategy
    {
        void Predict(IFeatureContext ctx, float deltaTime);
    }

    /// <summary>
    /// 线性预测策略
    /// </summary>
    public sealed class LinearPredictionStrategy : IPredictionStrategy
    {
        public void Predict(IFeatureContext ctx, float deltaTime)
        {
            // 线性预测
        }
    }

    /// <summary>
    /// 物理预测策略
    /// </summary>
    public sealed class PhysicsPredictionStrategy : IPredictionStrategy
    {
        public void Predict(IFeatureContext ctx, float deltaTime)
        {
            // 物理模拟预测
        }
    }

    /// <summary>
    /// 插值 Handler 示例
    /// </summary>
    public sealed class InterpolationHandler : HandlerBase
    {
        public override int Order => 300;

        public override void OnAttach(IFeatureContext ctx)
        {
            Platform.Log.View("[InterpolationHandler] Attached");
        }

        public override void OnDetach(IFeatureContext ctx)
        {
            Platform.Log.View("[InterpolationHandler] Detached");
        }

        public override void Handle(IFeatureContext ctx, float deltaTime)
        {
            // 插值逻辑
        }
    }
}
