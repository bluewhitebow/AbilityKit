using AbilityKit.Pipeline;

namespace ET.Logic
{
    /// <summary>
    /// 帧推进处理器
    /// 使用 BattleFramePipeline 统一管理帧处理流程
    /// 
    /// 架构:
    /// - PreTickPhase: 更新帧号和时间
    /// - CollectInputPhase: 收集输入
    /// - ExecuteCommandsPhase: 分发命令
    /// - DriveWorldPhase: 驱动 ECS 世界
    /// - CollectSnapshotPhase: 收集快照
    /// - DispatchSnapshotPhase: 分发快照
    /// - PostTickPhase: 后处理
    /// </summary>
    [LifecycleHandler(LifecyclePhase.Tick)]
    public sealed class TickHandler : ITickHandler
    {
        public LifecyclePhase Phase => LifecyclePhase.Tick;

        /// <summary>
        /// 战斗帧处理管线
        /// </summary>
        private BattleFramePipeline _pipeline;

        public void Handle(ETMobaBattleDriver driver, float deltaTime)
        {
            if (!driver.IsRunning)
                return;

            // 懒初始化管线
            _pipeline ??= new BattleFramePipeline();

            // 更新逻辑时间
            driver.LogicTimeSeconds += deltaTime;
            driver.LastTickTime = GetCurrentTimeSeconds();

            // 使用管线执行帧处理
            _pipeline.Tick(driver, deltaTime);
        }

        private static double GetCurrentTimeSeconds()
        {
            return (double)System.Environment.TickCount64 / 1000.0;
        }
    }
}
