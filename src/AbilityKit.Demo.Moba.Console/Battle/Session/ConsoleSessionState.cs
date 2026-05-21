using System;

namespace AbilityKit.Demo.Moba.Console.Battle.Session
{
    /// <summary>
    /// 战斗会话状态
    /// 对齐 Unity BattleSessionState
    /// </summary>
    public sealed class ConsoleSessionState
    {
        /// <summary>
        /// 最后处理的帧
        /// </summary>
        public int LastFrame { get; set; }

        /// <summary>
        /// Tick 累积时间
        /// </summary>
        public float TickAcc { get; set; }

        /// <summary>
        /// 是否已收到第一帧
        /// </summary>
        public bool FirstFrameReceived { get; set; }

        /// <summary>
        /// 会话是否处于活动状态
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 会话开始时间（秒）
        /// </summary>
        public double StartTimeSeconds { get; set; }

        /// <summary>
        /// 重置状态
        /// </summary>
        public void Reset()
        {
            LastFrame = 0;
            TickAcc = 0f;
            FirstFrameReceived = false;
            IsActive = false;
            StartTimeSeconds = 0d;
        }
    }
}
