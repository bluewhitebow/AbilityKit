# 4.1 视图事件抽象

> 理解 IBattleViewEventSink 如何解耦逻辑层与表现层。

---

## 目录

1. [视图抽象概述](#1-视图抽象概述)
2. [接口定义](#2-接口定义)
3. [事件类型](#3-事件类型)
4. [使用流程](#4-使用流程)

---

## 1. 视图抽象概述

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           视图抽象概述                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   视图抽象 = 逻辑层与表现层之间的"桥梁"                              │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐ │
│   │  核心思想                                                             │ │
│   │                                                                       │ │
│   │  逻辑层 (Logic) ──▶ IBattleViewEventSink ──▶ 表现层 (View)     │ │
│   │                                                                       │ │
│   │  • 逻辑层不知道 View 的实现                                         │ │
│   │  • View 层可以有多种实现（Console/Unity/Server）                  │ │
│   │  • 逻辑层只负责发送事件，不关心渲染                                 │ │
│   └─────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. 接口定义

```csharp
public interface IBattleViewEventSink
{
    // ========== 实体相关 ==========

    /// <summary>进入游戏快照</summary>
    void OnEnterGameSnapshot(in FrameSnapshotData snapshot);

    /// <summary>实体创建</summary>
    void OnActorSpawn(in ActorSpawnData spawn);

    /// <summary>实体销毁</summary>
    void OnActorDestroy(int actorId);

    /// <summary>实体移动</summary>
    void OnActorMove(in ActorMoveData move);

    // ========== 战斗相关 ==========

    /// <summary>受到伤害</summary>
    void OnActorDamage(in ActorDamageData damage);

    /// <summary>死亡</summary>
    void OnActorDead(in ActorDeadData dead);

    /// <summary>复活</summary>
    void OnActorRevive(in ActorReviveData revive);

    // ========== 技能相关 ==========

    /// <summary>技能释放</summary>
    void OnSkillCast(in SkillCastData cast);

    /// <summary>技能命中</summary>
    void OnSkillHit(in SkillHitData hit);

    // ========== Buff 相关 ==========

    /// <summary>Buff 应用</summary>
    void OnBuffApplied(in BuffAppliedData buff);

    /// <summary>Buff 移除</summary>
    void OnBuffRemoved(int actorId, int buffId);
}
```

---

## 3. 事件类型

```csharp
// 实体创建数据
public readonly struct ActorSpawnData
{
    public int ActorId;
    public string Name;
    public int HeroId;
    public Vec3 Position;
    public float Rotation;
    public int MaxHp;
    public int MaxMp;
}

// 实体移动数据
public readonly struct ActorMoveData
{
    public int ActorId;
    public Vec3 Position;
    public float Rotation;
}

// 伤害数据
public readonly struct ActorDamageData
{
    public int TargetId;
    public int AttackerId;
    public int Damage;
    public EDamageType DamageType;
    public int NewHp;
}

// 死亡数据
public readonly struct ActorDeadData
{
    public int ActorId;
    public int KillerId;
}
```

---

## 4. 使用流程

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           使用流程                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   1. 定义 View 实现                                                        │
│      ConsoleBattleView : IBattleViewEventSink                             │
│                                                                             │
│   2. 注册到容器                                                             │
│      container.RegisterSingleton<IBattleViewEventSink, ConsoleBattleView>(); │
│                                                                             │
│   3. 逻辑层发布事件                                                         │
│      viewSink.OnActorSpawn(spawnData);                                     │
│                                                                             │
│   4. View 层处理                                                           │
│      ConsoleBattleView.OnActorSpawn() → Console.WriteLine()               │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 下一步

- [快照分发](./02-SnapshotDispatch.md) - FrameSnapshotDispatcher
- [跨平台实现](./03-CrossPlatform.md) - Console/Unity/Server 实现

---

*文档版本：v1.0 | 最后更新：2026-06-21*
