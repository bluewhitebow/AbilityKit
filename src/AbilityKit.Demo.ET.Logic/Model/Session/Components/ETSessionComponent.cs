using System;

namespace ET.Logic
{
    /// <summary>
    /// 会话组件 - 管理战斗会话生命周期
    /// 对应 Moba.Console �?ConsoleSessionState + ConsoleSessionOrchestrator
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETSessionComponent: Entity, IAwake
    {
        // 会话状�?
        public bool IsActive { get; set; }
        public long StartTimeSeconds { get; set; }
        public int LastProcessedFrame { get; set; }
        public float TickAccumulator { get; set; }

        // 帧率配置
        public float FrameRate { get; set; } = 30f;
        public float FrameInterval => 1f / FrameRate;

        // 同步状�?
        public bool FirstFrameReceived { get; set; }
        public bool IsPaused { get; set; }
        public bool IsCatchingUp { get; set; }
        public int FrameDelay { get; set; }

        public void Awake()
        {
        }
    }
}
