# 8.2 触发器系统

> 理解条件-动作触发机制的设计与实现。

---

## 目录

1. [触发器概述](#1-触发器概述)
2. [触发计划结构](#2-触发计划结构)
3. [条件评估](#3-条件评估)
4. [动作执行](#4-动作执行)

---

## 1. 触发器概述

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           触发器概述                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   触发器 = 触发源 + 条件 + 动作                                        │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐ │
│   │  触发源 (Trigger)                                                      │ │
│   │  • OnCast           ─── 施法时触发                                   │ │
│   │  • OnHit            ─── 命中时触发                                   │ │
│   │  • OnProjectileHit  ─── 投射物命中时触发                           │ │
│   │  • OnTick           ─── 周期触发                                     │ │
│   └─────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. 触发计划结构

```csharp
public sealed class TriggerPlanConfig
{
    /// <summary>触发源</summary>
    public TriggerSourceConfig Trigger;

    /// <summary>触发条件</summary>
    public ConditionConfig[] Conditions;

    /// <summary>执行动作</summary>
    public ActionConfig[] Actions;
}
```

---

## 3. 条件评估

```csharp
public interface ICondition
{
    string Name { get; }
    ConditionResult Evaluate(TriggerContext context);
}

// 内置条件
// • TargetHasTag ─── 目标有某标签
// • RandomChance ─── 随机概率
// • HealthPercentBelow ─── 生命值低于百分比
```

---

## 4. 动作执行

```csharp
public interface IAction
{
    string Name { get; }
    ActionResult Execute(TriggerContext context);
}

// 内置动作
// • ApplyDamage ─── 造成伤害
// • ApplyBuff ─── 应用Buff
// • SpawnProjectile ─── 生成投射物
```

---

## 下一步

- [Buff系统](../03-BuffSystem.md) - Buff 生命周期
- [投射物系统](../04-ProjectileSystem.md) - 投射物实现

---

*文档版本：v1.0 | 最后更新：2026-06-21*
