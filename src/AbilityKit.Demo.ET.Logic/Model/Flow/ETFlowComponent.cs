using System;

namespace ET.Logic
{
    /// <summary>
    /// 流程组件 - 管理战斗流程状�?
    /// 对应 Moba.Console �?PhaseHost + InMatchPhase
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETFlowComponent: Entity, IAwake
    {
        // 流程状�?
        public FlowPhase CurrentPhase { get; set; } = FlowPhase.None;
        public FlowStep CurrentStep { get; set; } = FlowStep.None;

        // 流程数据
        public int StepsCompleted { get; set; }
        public bool IsTransitioning { get; set; }
        public float PhaseTimer { get; set; }

        public void Awake()
        {
        }
    }
}
