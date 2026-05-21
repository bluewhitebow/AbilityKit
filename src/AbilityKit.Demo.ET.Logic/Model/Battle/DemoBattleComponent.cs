using System;

namespace ET.AbilityKit.Demo.ET.Logic
{
    /// <summary>
    /// 单位类型
    /// </summary>
    public enum DemoUnitType
    {
        Hero,
        Monster,
        NPC,
    }

    /// <summary>
    /// 单位实体 - 只定义数据
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class DemoUnit: Entity, IAwake
    {
        public string Name { get; set; }
        public DemoUnitType UnitType { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Hp { get; set; }
        public float MaxHp { get; set; }

        public bool IsDead => Hp <= 0;

        public void Awake()
        {
        }
    }

    /// <summary>
    /// 单位管理器组件 - 只定义数据
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class DemoUnitComponent: Entity, IAwake
    {
        public void Awake()
        {
        }
    }

    /// <summary>
    /// 单位死亡事件
    /// </summary>
    public struct DemoUnitDead: IEvent
    {
        public Type Type => typeof(DemoUnitDead);

        public long UnitId;
    }

    /// <summary>
    /// 战斗状态
    /// </summary>
    public enum DemoBattleState
    {
        Idle,
        Loading,
        Ready,
        InProgress,
        Ended,
    }

    /// <summary>
    /// 战斗组件 - 只定义数据
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class DemoBattleComponent: Entity, IAwake, IUpdate
    {
        public long BattleId { get; set; }
        public long PlayerId { get; set; }
        public long PlayerUnitId { get; set; }
        public DemoBattleState State { get; set; } = DemoBattleState.Idle;
        public float BattleTime { get; set; }

        public void Awake()
        {
        }

        public void Update(DemoBattleComponent self)
        {
        }
    }

    /// <summary>
    /// 战斗场景初始化完成事件
    /// </summary>
    public struct DemoBattleSceneInitFinish: IEvent
    {
        public Type Type => typeof(DemoBattleSceneInitFinish);

        public long PlayerId;
        public string PlayerName;
        public long BattleId;
    }

    /// <summary>
    /// 战斗开始事件
    /// </summary>
    public struct DemoBattleStart: IEvent
    {
        public Type Type => typeof(DemoBattleStart);

        public long BattleId;
    }

    /// <summary>
    /// 战斗结束事件
    /// </summary>
    public struct BattleEnd: IEvent
    {
        public Type Type => typeof(BattleEnd);

        public long BattleId;
        public bool IsVictory;
    }
}
