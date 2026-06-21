# 8.5 属性系统

> 理解 Attributes 与 Modifiers 的设计与实现。

---

## 目录

1. [属性系统概述](#1-属性系统概述)
2. [属性定义](#2-属性定义)
3. [属性修饰器](#3-属性修饰器)
4. [属性计算](#4-属性计算)

---

## 1. 属性系统概述

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           属性系统概述                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   Attribute = 基础值 + 修饰器 = 最终值                                │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐ │
│   │  最终攻击力 = 基础攻击力 + Buff加成 + 装备加成 + ...               │ │
│   │                                                                       │ │
│   │  基础攻击力 ──▶ 100                                                │ │
│   │       │                                                             │ │
│   │       +                                                             │ │
│   │       ▼                                                             │ │
│   │  Buff加成 ──▶ +20 (攻击力+20% Buff)                              │ │
│   │       │                                                             │ │
│   │       +                                                             │ │
│   │       ▼                                                             │ │
│   │  装备加成 ──▶ +50 (装备提供的固定值)                              │ │
│   │       │                                                             │ │
│   │       ▼                                                             │ │
│   │  最终攻击力 ──▶ 170                                                │ │
│   └─────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. 属性定义

```csharp
public enum EAttributeType
{
    MaxHp, Hp,
    MaxMp, Mp,
    Attack, Defense,
    MagicAttack, MagicDefense,
    MoveSpeed, AttackSpeed,
    CritRate, CritDamage,
    DodgeRate, HitRate, BlockRate,
}

public struct ActorAttributeComponent : IECComponent
{
    public readonly int ActorId;
    public readonly int MaxHp;
    public readonly int Hp;
    public readonly int Attack;
    public readonly int Defense;
    // ... 其他属性
}
```

---

## 3. 属性修饰器

```csharp
public readonly struct AttributeModifier
{
    public readonly int ModifierId;
    public readonly int SourceId;
    public readonly EModifierSourceType SourceType;
    public readonly EAttributeType AttributeType;
    public readonly EModifierType ModifierType;
    public readonly float Value;
}

public enum EModifierType
{
    Add,              // 固定值加
    PercentAdd,       // 百分比加成（基于基础值）
    PercentBase,      // 百分比加成（基于最终值）
}
```

---

## 4. 属性计算

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           属性计算公式                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   最终值 = (基础值 + Σ固定值加成) × (1 + Σ百分比加成 / 100)         │
│                                                                             │
│   示例：                                                                  │
│   基础攻击力 = 100                                                        │
│   修饰器列表：                                                            │
│   • Buff "狂暴": PercentAdd, Attack, 20%                                │
│   • Buff "力量": Add, Attack, 50                                        │
│   • Equipment "长剑": PercentAdd, Attack, 10%                            │
│                                                                             │
│   计算过程：                                                              │
│   1. 固定值加成 = 50                                                    │
│   2. 百分比加成 = 20% + 10% = 30%                                       │
│   3. 最终值 = (100 + 50) × (1 + 30%) = 150 × 1.3 = 195                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 下一步

- [伤害计算](../06-DamageCalculation.md) - 伤害公式与护甲减伤
- [技能系统架构](../01-SkillSystemArchitecture.md) - 技能流程

---

*文档版本：v1.0 | 最后更新：2026-06-21*
