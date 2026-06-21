# 8.4 投射物系统

> 理解投射物从创建到命中的完整流程。

---

## 目录

1. [投射物概述](#1-投射物概述)
2. [投射物配置](#2-投射物配置)
3. [弹道类型](#3-弹道类型)
4. [命中检测](#4-命中检测)

---

## 1. 投射物概述

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           投射物概述                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   投射物 = 从施法者飞向目标的"子弹"                                  │
│                                                                             │
│   生命周期：                                                              │
│   施法者 ──▶ Launch ──▶ [飞行] ──▶ Hit ──▶ 触发效果              │
│                               │                                          │
│                               └── [Miss] ──▶ 消失                      │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. 投射物配置

```csharp
public sealed class ProjectileConfig
{
    public int ProjectileId;
    public string Name;
    public float Speed;
    public EProjectileType Type;
    public ETrajectoryType Trajectory;
    public float CollisionRadius;
    public float MaxDistance;
    public float MaxLifetime;
    public bool Blockable;
    public bool Homing;
}
```

---

## 3. 弹道类型

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           弹道类型                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   Direct (直线弹道)                                                       │
│   └── 简单直接，不追踪目标                                               │
│                                                                             │
│   Homing (追踪弹道)                                                      │
│   └── 会追踪移动目标，但可能被闪避                                       │
│                                                                             │
│   Arc (抛物线弹道)                                                        │
│   └── 抛物线轨迹，可越过障碍                                            │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. 命中检测

```csharp
public sealed class ProjectileHitSystem : IECSystem
{
    public void Execute()
    {
        var projectiles = _world.GetEntities(
            Matcher.AllOf<ProjectileInstance>(),
            Matcher.NoneOf<ProjectileHitComponent>()
        );

        foreach (var entity in projectiles)
        {
            var projectile = entity.GetComponent<ProjectileInstance>();

            if (CheckHit(projectile, out var hitTarget))
            {
                entity.AddComponent(new ProjectileHitComponent { TargetId = hitTarget });
            }
        }
    }
}
```

---

## 下一步

- [属性系统](../05-AttributeSystem.md) - Attributes 与 Modifiers
- [伤害计算](../06-DamageCalculation.md) - 伤害公式

---

*文档版本：v1.0 | 最后更新：2026-06-21*
