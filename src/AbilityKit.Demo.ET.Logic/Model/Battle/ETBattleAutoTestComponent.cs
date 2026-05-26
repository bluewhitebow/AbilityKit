using System;

namespace ET.Logic
{
    /// <summary>
    /// 战斗自动测试组件（纯数据）
    ///
    /// 职责：
    /// - 仅存储测试参数和状态
    /// - 业务逻辑由 ETBattleAutoTestComponentSystem 处理
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETBattleAutoTestComponent : Entity, IAwake, IUpdate, IDestroy
    {
        // ========== 测试参数 ==========

        /// <summary>
        /// 是否启用自动测试
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 测试的 ActorId（运行时自增 ID）
        /// </summary>
        public int TestActorId { get; set; }

        /// <summary>
        /// 移动命令间隔帧数
        /// </summary>
        public int MoveIntervalFrames { get; set; } = BattleTestConfig.DefaultMoveIntervalFrames;

        /// <summary>
        /// 移动速度
        /// </summary>
        public float MoveSpeed { get; set; } = BattleTestConfig.DefaultMoveSpeed;

        /// <summary>
        /// 目标 X
        /// </summary>
        public float TargetX { get; set; } = BattleTestConfig.DefaultTargetX;

        /// <summary>
        /// 目标 Y
        /// </summary>
        public float TargetY { get; set; } = BattleTestConfig.DefaultTargetY;

        // ========== 统计信息 ==========

        /// <summary>
        /// 移动命令计数
        /// </summary>
        public int MoveCommandCount { get; set; }

        /// <summary>
        /// 当前 X
        /// </summary>
        public float CurrentX { get; set; }

        /// <summary>
        /// 当前 Y
        /// </summary>
        public float CurrentY { get; set; }

        /// <summary>
        /// 移动总距离
        /// </summary>
        public float MoveDistance { get; set; }

        /// <summary>
        /// 初始化测试
        /// </summary>
        public void Initialize(int actorId, float startX, float startY)
        {
            TestActorId = actorId;
            CurrentX = startX;
            CurrentY = startY;
            MoveCommandCount = 0;
            MoveDistance = 0f;
        }

        public void Awake()
        {
        }

        public void Destroy()
        {
        }
    }
}
