# 8.1 技能系统架构

> 理解技能从配置到执行的完整流程。

---

## 目录

1. [技能系统概述](#1-技能系统概述)
2. [技能配置](#2-技能配置)
3. [技能执行管线](#3-技能执行管线)
4. [技能验证](#4-技能验证)

---

## 1. 技能系统概述

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           技能系统架构                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐ │
│   │  配置层                                                              │ │
│   │  SkillConfig ──▶ SkillPipelineConfig ──▶ TriggerPlanConfig          │ │
│   └─────────────────────────────────────────────────────────────────────┘ │
│                                    │                                        │
│                                    ▼                                        │
│   ┌─────────────────────────────────────────────────────────────────────┐ │
│   │  执行层                                                              │ │
│   │  SkillExecutor ──▶ Pipeline ──▶ TriggerRunner                      │ │
│   └─────────────────────────────────────────────────────────────────────┘ │
│                                    │                                        │
│                                    ▼                                        │
│   ┌─────────────────────────────────────────────────────────────────────┐ │
│   │  效果层                                                              │ │
│   │  Damage ──▶ Buff ──▶ Projectile ──▶ Vfx                           │ │
│   └─────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. 技能配置

```csharp
public sealed class SkillConfig
{
    public int SkillId;
    public string Name;
    public int MaxLevel;
    public float Cooldown;
    public int ManaCost;
    public float CastRange;
    public ESkillTargetType TargetType;
    public ESkillCastType CastType;
    public SkillPipelineConfig Pipeline;
    public TriggerPlanConfig[] TriggerPlans;
}
```

---

## 3. 技能执行管线

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           技能执行管线                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   Validation Phase ──▶ PreCast ──▶ Cast ──▶ PostCast                   │
│                                                                             │
│   ├── Validation: 冷却检查、魔法值检查、距离检查                         │
│   ├── PreCast:     消耗资源、设置冷却、播放动画                         │
│   ├── Cast:        执行效果、触发计划                                    │
│   └── PostCast:    重置状态、清理临时数据                               │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. 技能验证

```csharp
public sealed class SkillValidationService
{
    public ValidationResult Validate(SkillCastContext context, SkillConfig config)
    {
        if (!CheckCooldown(context.CasterId, context.SkillId))
            return ValidationResult.Fail("Cooldown not ready");

        if (!CheckMana(context.CasterId, config.ManaCost))
            return ValidationResult.Fail("Not enough mana");

        if (!CheckRange(context, config.CastRange))
            return ValidationResult.Fail("Target out of range");

        return ValidationResult.Success();
    }
}
```

---

## 下一步

- [触发器系统](../02-TriggeringSystem.md) - 条件-动作机制
- [Buff系统](../03-BuffSystem.md) - Buff 生命周期

---

*文档版本：v1.0 | 最后更新：2026-06-21*
