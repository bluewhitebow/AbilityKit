namespace AbilityKit.Demo.Moba.Console.Services
{
    /// <summary>
    /// Moba 操作码定义
    /// </summary>
    public enum MobaOpCode
    {
        /// <summary>
        /// 就绪
        /// </summary>
        Ready = 3001,

        /// <summary>
        /// 取消就绪
        /// </summary>
        Unready = 3002,

        /// <summary>
        /// 移动
        /// </summary>
        Move = 3003,

        // ========== 技能操作码 ==========

        /// <summary>
        /// 技能1
        /// </summary>
        Skill1 = 3011,

        /// <summary>
        /// 技能2
        /// </summary>
        Skill2 = 3012,

        /// <summary>
        /// 技能3
        /// </summary>
        Skill3 = 3013,

        /// <summary>
        /// 通用技能输入（支持瞄准等）
        /// </summary>
        SkillInput = 3020,

        // ========== 快照操作码 ==========

        /// <summary>
        /// 大厅快照
        /// </summary>
        LobbySnapshot = 4001,

        /// <summary>
        /// 进入游戏快照
        /// </summary>
        EnterGameSnapshot = 4002,

        /// <summary>
        /// 角色位置快照
        /// </summary>
        ActorTransformSnapshot = 4003,

        /// <summary>
        /// 状态哈希快照
        /// </summary>
        StateHashSnapshot = 4004,

        /// <summary>
        /// 角色生成快照
        /// </summary>
        ActorSpawnSnapshot = 4005,

        /// <summary>
        /// 弹道事件快照
        /// </summary>
        ProjectileEventSnapshot = 4006,

        /// <summary>
        /// 伤害事件快照
        /// </summary>
        DamageEventSnapshot = 4007,

        /// <summary>
        /// 角色销毁快照
        /// </summary>
        ActorDespawnSnapshot = 4008,

        /// <summary>
        /// 区域事件快照
        /// </summary>
        AreaEventSnapshot = 4009,
    }

    /// <summary>
    /// 技能输入阶段
    /// </summary>
    public enum SkillInputPhase
    {
        /// <summary>
        /// 按下
        /// </summary>
        Press = 1,

        /// <summary>
        /// 按住
        /// </summary>
        Hold = 2,

        /// <summary>
        /// 释放
        /// </summary>
        Release = 3,

        /// <summary>
        /// 取消
        /// </summary>
        Cancel = 4,
    }

    /// <summary>
    /// 技能施放阶段
    /// </summary>
    public enum SkillCastStage
    {
        /// <summary>
        /// 前摇
        /// </summary>
        PreCast = 0,

        /// <summary>
        /// 施法中
        /// </summary>
        Cast = 1,

        /// <summary>
        /// 时间轴
        /// </summary>
        Timeline = 2,

        /// <summary>
        /// 已完成
        /// </summary>
        Completed = 3,

        /// <summary>
        /// 已中断
        /// </summary>
        Interrupted = 4,
    }
}
