using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Share
{
    /// <summary>
    /// 战斗驱动接口
    /// 定义平台无关的战斗驱动契约
    ///
    /// 这个接口不依赖任何 ECS 实现，由外部宿主（如 ET.Game、Console 等）实现
    /// 负责：
    /// 1. 管理战斗生命周期（创建、开始、结束）
    /// 2. 驱动帧循环（Tick）
    /// 3. 管理 Actor 状态
    /// 4. 处理输入
    /// 5. 产生视图事件
    ///
    /// moba.core 本身不集成 ECS，通过这个接口由外部宿主驱动
    /// </summary>
    public interface IBattleDriver
    {
        // ============== 属性 ==============

        /// <summary>
        /// 当前帧索引
        /// </summary>
        int CurrentFrame { get; }

        /// <summary>
        /// 总逻辑时间（秒）
        /// </summary>
        double LogicTimeSeconds { get; }

        /// <summary>
        /// 帧率（每秒帧数）
        /// </summary>
        int TickRate { get; }

        /// <summary>
        /// 是否正在运行
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// 战斗视图事件接收器
        /// </summary>
        IBattleViewEventSink ViewEventSink { get; set; }

        /// <summary>
        /// 启动计划
        /// </summary>
        BattleStartPlan Plan { get; }

        // ============== 生命周期 ==============

        /// <summary>
        /// 初始化战斗驱动
        /// </summary>
        /// <param name="plan">启动计划</param>
        /// <param name="viewSink">视图事件接收器</param>
        void Initialize(in BattleStartPlan plan, IBattleViewEventSink viewSink);

        /// <summary>
        /// 启动战斗
        /// </summary>
        void Start();

        /// <summary>
        /// 停止战斗
        /// </summary>
        void Stop();

        /// <summary>
        /// 销毁战斗驱动
        /// </summary>
        void Destroy();

        // ============== 帧循环 ==============

        /// <summary>
        /// 执行一帧
        /// 由外部宿主按固定频率调用
        /// </summary>
        /// <param name="deltaTime">上一帧到当前的时间（秒）</param>
        void Tick(float deltaTime);

        // ============== Actor 管理 ==============

        /// <summary>
        /// 创建 Actor
        /// </summary>
        /// <param name="actorId">Actor ID</param>
        /// <param name="characterId">角色配置 ID</param>
        /// <param name="teamId">队伍 ID</param>
        /// <param name="x">初始 X 坐标</param>
        /// <param name="y">初始 Y 坐标</param>
        /// <param name="z">初始 Z 坐标</param>
        void CreateActor(int actorId, int characterId, int teamId, float x, float y, float z);

        /// <summary>
        /// 获取 Actor 变换数据
        /// </summary>
        /// <param name="actorId">Actor ID</param>
        /// <returns>变换数据，如果 Actor 不存在返回 null</returns>
        ActorTransformData? GetActorTransform(int actorId);

        /// <summary>
        /// 获取所有 Actor 变换数据
        /// </summary>
        /// <returns>所有 Actor 的变换数据</returns>
        IReadOnlyList<ActorTransformData> GetAllActorTransforms();

        /// <summary>
        /// 获取所有活着的 Actor ID
        /// </summary>
        /// <returns>活着的 Actor ID 列表</returns>
        IReadOnlyList<int> GetAliveActorIds();

        /// <summary>
        /// 获取 Actor 属性
        /// </summary>
        /// <param name="actorId">Actor ID</param>
        /// <param name="attributeType">属性类型</param>
        /// <returns>属性值</returns>
        float GetActorAttribute(int actorId, ActorAttributeType attributeType);

        /// <summary>
        /// 设置 Actor 属性
        /// </summary>
        /// <param name="actorId">Actor ID</param>
        /// <param name="attributeType">属性类型</param>
        /// <param name="value">属性值</param>
        void SetActorAttribute(int actorId, ActorAttributeType attributeType, float value);

        /// <summary>
        /// 修改 Actor 属性
        /// </summary>
        /// <param name="actorId">Actor ID</param>
        /// <param name="attributeType">属性类型</param>
        /// <param name="delta">变化值（可以为负）</param>
        /// <returns>修改后的值</returns>
        float ModifyActorAttribute(int actorId, ActorAttributeType attributeType, float delta);

        /// <summary>
        /// 检查 Actor 是否死亡
        /// </summary>
        /// <param name="actorId">Actor ID</param>
        /// <returns>是否死亡</returns>
        bool IsActorDead(int actorId);

        /// <summary>
        /// 标记 Actor 死亡
        /// </summary>
        /// <param name="actorId">Actor ID</param>
        /// <param name="killerId">击杀者 ID</param>
        void MarkActorDead(int actorId, int killerId);

        /// <summary>
        /// 移动 Actor
        /// </summary>
        /// <param name="actorId">Actor ID</param>
        /// <param name="targetX">目标 X 坐标</param>
        /// <param name="targetZ">目标 Z 坐标</param>
        void MoveActor(int actorId, float targetX, float targetZ);

        // ============== 技能系统 ==============

        /// <summary>
        /// 检查技能是否可以释放
        /// </summary>
        /// <param name="actorId">释放者 Actor ID</param>
        /// <param name="slot">技能槽位</param>
        /// <returns>是否可以释放</returns>
        bool CanCastSkill(int actorId, int slot);

        /// <summary>
        /// 释放技能（目标点）
        /// </summary>
        /// <param name="actorId">释放者 Actor ID</param>
        /// <param name="slot">技能槽位</param>
        /// <param name="targetX">目标 X 坐标</param>
        /// <param name="targetZ">目标 Z 坐标</param>
        /// <returns>是否成功释放</returns>
        bool CastSkill(int actorId, int slot, float targetX, float targetZ);

        /// <summary>
        /// 释放技能（目标单位）
        /// </summary>
        /// <param name="actorId">释放者 Actor ID</param>
        /// <param name="slot">技能槽位</param>
        /// <param name="targetActorId">目标 Actor ID</param>
        /// <returns>是否成功释放</returns>
        bool CastSkillOnTarget(int actorId, int slot, int targetActorId);

        /// <summary>
        /// 检查技能冷却
        /// </summary>
        /// <param name="actorId">Actor ID</param>
        /// <param name="slot">技能槽位</param>
        /// <returns>剩余冷却时间（秒），0 表示冷却完成</returns>
        float GetSkillCooldown(int actorId, int slot);

        /// <summary>
        /// 检查技能是否可用
        /// </summary>
        /// <param name="actorId">Actor ID</param>
        /// <param name="slot">技能槽位</param>
        /// <returns>是否可用（冷却完成且有足够资源）</returns>
        bool IsSkillReady(int actorId, int slot);

        // ============== Buff 系统 ==============

        /// <summary>
        /// 添加 Buff
        /// </summary>
        /// <param name="actorId">目标 Actor ID</param>
        /// <param name="casterId">释放者 Actor ID</param>
        /// <param name="buffId">Buff 配置 ID</param>
        /// <returns>Buff 实例 ID，-1 表示添加失败</returns>
        int AddBuff(int actorId, int casterId, int buffId);

        /// <summary>
        /// 移除 Buff
        /// </summary>
        /// <param name="actorId">目标 Actor ID</param>
        /// <param name="buffInstanceId">Buff 实例 ID</param>
        void RemoveBuff(int actorId, int buffInstanceId);

        /// <summary>
        /// 获取 Actor 的 Buff 数量
        /// </summary>
        /// <param name="actorId">Actor ID</param>
        /// <param name="buffId">Buff 配置 ID</param>
        /// <returns>Buff 层数</returns>
        int GetBuffStack(int actorId, int buffId);

        // ============== 查询 ==============

        /// <summary>
        /// 查找范围内的 Actor
        /// </summary>
        /// <param name="x">圆心 X 坐标</param>
        /// <param name="z">圆心 Z 坐标</param>
        /// <param name="radius">半径</param>
        /// <param name="teamFilter">队伍过滤（-1 表示所有队伍）</param>
        /// <returns>符合条件的 Actor ID 列表</returns>
        IReadOnlyList<int> FindActorsInRange(float x, float z, float radius, int teamFilter = -1);

        /// <summary>
        /// 查找最近的 Actor
        /// </summary>
        /// <param name="x">原点 X 坐标</param>
        /// <param name="z">原点 Z 坐标</param>
        /// <param name="radius">最大搜索半径</param>
        /// <param name="teamFilter">队伍过滤（-1 表示所有队伍）</param>
        /// <returns>最近的 Actor ID，不存在返回 -1</returns>
        int FindNearestActor(float x, float z, float radius, int teamFilter = -1);

        // ============== 伤害系统 ==============

        /// <summary>
        /// 造成伤害
        /// </summary>
        /// <param name="attackerId">攻击者 Actor ID</param>
        /// <param name="targetId">目标 Actor ID</param>
        /// <param name="damage">伤害值</param>
        /// <param name="damageType">伤害类型</param>
        /// <returns>实际造成的伤害值</returns>
        float ApplyDamage(int attackerId, int targetId, float damage, int damageType);

        /// <summary>
        /// 造成治疗
        /// </summary>
        /// <param name="healerId">治疗者 Actor ID</param>
        /// <param name="targetId">目标 Actor ID</param>
        /// <param name="heal">治疗值</param>
        /// <returns>实际治疗量</returns>
        float ApplyHeal(int healerId, int targetId, float heal);
    }

    /// <summary>
    /// Actor 属性类型
    /// </summary>
    public enum ActorAttributeType
    {
        MaxHp = 0,
        Hp = 1,
        MaxMp = 2,
        Mp = 3,
        Attack = 4,
        Defense = 5,
        MoveSpeed = 6,
        AttackSpeed = 7,
        CritRate = 8,
        CritDamage = 9,
        Armor = 10,
        MagicResist = 11,
        HpRegen = 12,
        MpRegen = 13,
    }

    /// <summary>
    /// 伤害类型
    /// </summary>
    public enum DamageType
    {
        Physical = 0,
        Magic = 1,
        True = 2,
    }

    /// <summary>
    /// 队伍关系
    /// </summary>
    public enum TeamRelation
    {
        Self = 0,
        Friend = 1,
        Enemy = 2,
        Neutral = 3,
    }

    /// <summary>
    /// 战斗驱动接口扩展
    /// 提供默认值实现和辅助方法
    /// </summary>
    public static class BattleDriverExtensions
    {
        /// <summary>
        /// 获取队伍关系
        /// </summary>
        public static TeamRelation GetTeamRelation(this IBattleDriver driver, int actorId1, int actorId2)
        {
            // 简单实现：假设每个 Actor 有 teamId 属性
            var team1 = (int)driver.GetActorAttribute(actorId1, ActorAttributeType.MaxHp); // 临时用 MaxHp 存储 teamId
            var team2 = (int)driver.GetActorAttribute(actorId2, ActorAttributeType.MaxHp);

            if (team1 == team2) return TeamRelation.Friend;
            if (team1 == 0 || team2 == 0) return TeamRelation.Neutral;
            return TeamRelation.Enemy;
        }

        /// <summary>
        /// 计算距离（2D）
        /// </summary>
        public static float Distance2D(float x1, float z1, float x2, float z2)
        {
            float dx = x2 - x1;
            float dz = z2 - z1;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>
        /// 计算两点之间的距离
        /// </summary>
        public static float Distance2D(this ActorTransformData self, float x, float z)
        {
            return Distance2D(self.PositionX, self.PositionZ, x, z);
        }

        /// <summary>
        /// 计算两个变换之间的距离
        /// </summary>
        public static float Distance2D(this ActorTransformData self, ActorTransformData other)
        {
            return Distance2D(self.PositionX, self.PositionZ, other.PositionX, other.PositionZ);
        }
    }
}
