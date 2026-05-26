using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// ???? System
    /// ?? Moba.Console ??ConsoleSessionOrchestrator
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
        /// ?????
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
        /// ????
        /// </summary>
        public static void StopSession(this ETSessionComponent self)
        {
            self.IsActive = false;
            Log.Info("[ETSession] Session stopped");
        }

        /// <summary>
        /// ????????
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
        /// Tick - ??????
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
        /// ?????
        /// </summary>
        private static void AdvanceFrame(this ETSessionComponent self)
        {
            self.LastProcessedFrame++;

            var battleComponent = self.Scene().GetComponent<ETBattleComponent>();
            var flowComponent = self.Scene().GetComponent<ETFlowComponent>();

            // ????
            flowComponent?.Tick(self.FrameInterval);

            // ??????
            if (battleComponent?.State == BattleState.InProgress)
            {
                battleComponent.AdvanceFrame();
            }
        }

        /// <summary>
        /// ??
        /// </summary>
        public static void Pause(this ETSessionComponent self)
        {
            self.IsPaused = true;
            Log.Info("[ETSession] Session paused");
        }

        /// <summary>
        /// ??
        /// </summary>
        public static void Resume(this ETSessionComponent self)
        {
            self.IsPaused = false;
            Log.Info("[ETSession] Session resumed");
        }

        /// <summary>
        /// ????
        /// </summary>
        public static void SetFrameRate(this ETSessionComponent self, float fps)
        {
            self.FrameRate = fps;
            Log.Info($"[ETSession] Frame rate set to {fps}");
        }
    }
}
