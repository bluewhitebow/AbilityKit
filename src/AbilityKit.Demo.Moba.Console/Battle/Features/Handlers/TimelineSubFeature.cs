using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.Battle.Flow;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.Battle.Features
{
    /// <summary>
    /// Timeline SubFeature（新架构）
    /// 使用 Handler 架构组织时间轴相关逻辑
    ///
    /// 层级结构：
    /// TimelineSubFeature
    /// ├── SimulationSubFeature (子模块)
    /// │   ├── InputHandler (Order=100)
    /// │   ├── PredictionHandler (Order=200)
    /// │   └── ReconciliationHandler (Order=300)
    /// └── ViewSyncSubFeature (子模块)
    ///     ├── InterpolationHandler (Order=400)
    ///     └── ExtrapolationHandler (Order=500)
    /// </summary>
    public sealed class TimelineSubFeature : SubFeatureBase
    {
        public override string Id => "timeline_sub_feature";
        public override string[] Dependencies => new[] { "binding_sub_feature" };
        public override int Priority => 100;

        public TimelineSubFeature()
        {
            // 添加子模块
            AddChild(new SimulationSubFeature());
            AddChild(new ViewSyncSubFeature());

            // 添加 Handler
            AddHandler(new FrameSyncHandler());
        }

        protected override void OnAttachInternal(IFeatureContext ctx)
        {
            Platform.Log.View("[TimelineSubFeature] Attached with Handler architecture");
        }

        protected override void OnDetachInternal(IFeatureContext ctx)
        {
            Platform.Log.View("[TimelineSubFeature] Detached");
        }
    }

    /// <summary>
    /// 模拟子模块
    /// 管理输入、预测、矫正等 Handler
    /// </summary>
    public sealed class SimulationSubFeature : SubFeatureBase
    {
        public override string Id => "simulation_sub_feature";
        public override int Priority => 110;

        public SimulationSubFeature()
        {
            // 按 Order 添加 Handler
            AddHandler(new InputHandler());           // Order = 100
            AddHandler(new PredictionHandler());       // Order = 200
            AddHandler(new ReconciliationHandler());  // Order = 300
        }

        protected override void OnAttachInternal(IFeatureContext ctx)
        {
            Platform.Log.View("[SimulationSubFeature] Attached");
        }
    }

    /// <summary>
    /// 视图同步子模块
    /// 管理插值、外推等 Handler
    /// </summary>
    public sealed class ViewSyncSubFeature : SubFeatureBase
    {
        public override string Id => "view_sync_sub_feature";
        public override int Priority => 120;

        public ViewSyncSubFeature()
        {
            // 按 Order 添加 Handler
            AddHandler(new InterpolationHandler());    // Order = 400
            AddHandler(new ExtrapolationHandler());   // Order = 500
        }

        protected override void OnAttachInternal(IFeatureContext ctx)
        {
            Platform.Log.View("[ViewSyncSubFeature] Attached");
        }
    }

    #region Handler 实现

    /// <summary>
    /// 输入 Handler
    /// </summary>
    public sealed class InputHandler : HandlerBase
    {
        public override int Order => 100;

        public override void OnAttach(IFeatureContext ctx)
        {
            Platform.Log.View("[InputHandler] Attached");
        }

        public override void Handle(IFeatureContext ctx, float deltaTime)
        {
            // 处理输入
        }
    }

    /// <summary>
    /// 预测 Handler
    /// </summary>
    public sealed class ReconciliationHandler : HandlerBase
    {
        public override int Order => 300;

        public override void OnAttach(IFeatureContext ctx)
        {
            Platform.Log.View("[ReconciliationHandler] Attached");
        }

        public override void Handle(IFeatureContext ctx, float deltaTime)
        {
            // 矫正逻辑
        }
    }

    /// <summary>
    /// 外推 Handler
    /// </summary>
    public sealed class ExtrapolationHandler : HandlerBase
    {
        public override int Order => 500;

        public override void OnAttach(IFeatureContext ctx)
        {
            Platform.Log.View("[ExtrapolationHandler] Attached");
        }

        public override void Handle(IFeatureContext ctx, float deltaTime)
        {
            // 外推逻辑
        }
    }

    #endregion
}
