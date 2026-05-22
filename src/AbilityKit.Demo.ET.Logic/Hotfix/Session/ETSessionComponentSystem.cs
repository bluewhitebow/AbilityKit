using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// 会话组件 System
    /// 对应 Moba.Console �?ConsoleSessionOrchestrator
    /// </summary>
    [EntitySystemOf(typeof(ETSessionComponent))]
    [FriendOf(typeof(ETSessionComponent))]
    [FriendOf(typeof(ETBattleComponent))]
    [FriendOf(typeof(ETFlowComponent))]
    public static partial class ETSessionComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ETSessionComponent self)
        {
            Log.Info("[ETSession] ETSessionComponent awake");
        }

        /// <summary>
        /// 开始会�?
        /// </summary>
        public static void StartSession(this ETSessionComponent self)
        {
            self.IsActive = true;
            self.StartTimeSeconds = Environment.TickCount64;
            self.LastProcessedFrame = 0;
            self.TickAccumulator = 0f;
            self.FirstFrameReceived = false;

            Log.Info("[ETSession] Session started");
        }

        /// <summary>
        /// 停止会话
        /// </summary>
        public static void StopSession(this ETSessionComponent self)
        {
            self.IsActive = false;
            Log.Info("[ETSession] Session stopped");
        }

        /// <summary>
        /// 标记首帧已接�?
        /// </summary>
        public static void MarkFirstFrameReceived(this ETSessionComponent self)
        {
            if (!self.FirstFrameReceived)
            {
                self.FirstFrameReceived = true;
                Log.Info("[ETSession] First frame received");
            }
        }

        /// <summary>
        /// Tick - 帧同步循�?
        /// </summary>
        public static void Tick(this ETSessionComponent self, float deltaTime)
        {
            if (!self.IsActive || self.IsPaused)
                return;

            self.TickAccumulator += deltaTime;

            while (self.TickAccumulator >= self.FrameInterval)
            {
                self.TickAccumulator -= self.FrameInterval;
                self.AdvanceFrame();
            }
        }

        /// <summary>
        /// 推进一�?
        /// </summary>
        private static void AdvanceFrame(this ETSessionComponent self)
        {
            self.LastProcessedFrame++;

            var battleComponent = self.Scene().GetComponent<ETBattleComponent>();
            var flowComponent = self.Scene().GetComponent<ETFlowComponent>();

            // 处理流程
            flowComponent?.Tick(self.FrameInterval);

            // 处理战斗�?
            if (battleComponent?.State == BattleState.InProgress)
            {
                battleComponent.AdvanceFrame();
            }
        }

        /// <summary>
        /// 暂停
        /// </summary>
        public static void Pause(this ETSessionComponent self)
        {
            self.IsPaused = true;
            Log.Info("[ETSession] Session paused");
        }

        /// <summary>
        /// 恢复
        /// </summary>
        public static void Resume(this ETSessionComponent self)
        {
            self.IsPaused = false;
            Log.Info("[ETSession] Session resumed");
        }

        /// <summary>
        /// 设置帧率
        /// </summary>
        public static void SetFrameRate(this ETSessionComponent self, float fps)
        {
            self.FrameRate = fps;
            Log.Info($"[ETSession] Frame rate set to {fps}");
        }
    }
}
