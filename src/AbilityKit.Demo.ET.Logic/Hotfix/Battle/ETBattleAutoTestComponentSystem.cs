using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// ETBattleAutoTestComponent System
    /// 处理所有自动测试业务逻辑
    /// </summary>
    [EntitySystemOf(typeof(ETBattleAutoTestComponent))]
    [FriendOf(typeof(ETBattleAutoTestComponent))]
    [FriendOf(typeof(ETInputComponent))]
    public static partial class ETBattleAutoTestComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ETBattleAutoTestComponent self)
        {
        }

        [EntitySystem]
        private static void Update(this ETBattleAutoTestComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this ETBattleAutoTestComponent self)
        {
            Log.Info($"[ETBattleAutoTest] Destroyed. Total move commands: {self.MoveCommandCount}, Distance: {self.MoveDistance:F2}");
        }

        /// <summary>
        /// 初始化自动测试
        /// </summary>
        public static void Initialize(this ETBattleAutoTestComponent self, long actorId, float startX, float startY)
        {
            self.Initialize(actorId, startX, startY);
            Log.Info($"[ETBattleAutoTest] Initialized for ActorId={actorId}, StartPos=({startX}, {startY})");
        }

        /// <summary>
        /// 每帧更新 - 发送自动移动命令
        /// </summary>
        public static void OnUpdate(this ETBattleAutoTestComponent self, int frame)
        {
            if (!self.IsEnabled)
                return;

            // 每隔指定帧数发送移动命令
            if (frame % self.MoveIntervalFrames == 0)
            {
                SendMoveCommand(self, frame);
            }
        }

        /// <summary>
        /// 发送移动命令
        /// </summary>
        private static void SendMoveCommand(ETBattleAutoTestComponent self, int frame)
        {
            // 计算新目标（基于时间动态计算）
            float newTargetX = self.CurrentX + (float)Math.Sin(frame * 0.1) * BattleTestConfig.MovementAmplitude;
            float newTargetY = self.CurrentY + (float)Math.Cos(frame * 0.1) * BattleTestConfig.MovementAmplitude;

            // 限制范围
            newTargetX = Math.Clamp(newTargetX, BattleTestConfig.MovementMinBound, BattleTestConfig.MovementMaxBound);
            newTargetY = Math.Clamp(newTargetY, BattleTestConfig.MovementMinBound, BattleTestConfig.MovementMaxBound);

            // 获取输入组件并发送命令
            var inputComponent = self.Scene().GetComponent<ETInputComponent>();
            if (inputComponent != null)
            {
                inputComponent.AddMoveCommand(frame, self.TestActorId, newTargetX, newTargetY);
                self.MoveCommandCount++;

                // 计算移动距离
                float dx = newTargetX - self.CurrentX;
                float dy = newTargetY - self.CurrentY;
                self.MoveDistance += (float)Math.Sqrt(dx * dx + dy * dy);

                // 更新当前位置
                self.CurrentX = newTargetX;
                self.CurrentY = newTargetY;

                Log.Info($"[ETBattleAutoTest] Move command: Frame={frame}, ActorId={self.TestActorId}, Target=({newTargetX:F2}, {newTargetY:F2})");
            }
            else
            {
                Log.Warning("[ETBattleAutoTest] ETInputComponent not found!");
            }
        }

        /// <summary>
        /// 启用自动测试
        /// </summary>
        public static void Enable(this ETBattleAutoTestComponent self)
        {
            self.IsEnabled = true;
            Log.Info("[ETBattleAutoTest] Auto test enabled");
        }

        /// <summary>
        /// 禁用自动测试
        /// </summary>
        public static void Disable(this ETBattleAutoTestComponent self)
        {
            self.IsEnabled = false;
            Log.Info("[ETBattleAutoTest] Auto test disabled");
        }

        /// <summary>
        /// 重置统计
        /// </summary>
        public static void ResetStats(this ETBattleAutoTestComponent self)
        {
            Log.Info($"[ETBattleAutoTest] Stats reset. Previous: Commands={self.MoveCommandCount}, Distance={self.MoveDistance:F2}");
            self.MoveCommandCount = 0;
            self.MoveDistance = 0f;
        }
    }
}
