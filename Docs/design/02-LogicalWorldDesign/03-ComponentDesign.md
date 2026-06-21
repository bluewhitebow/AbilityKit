# 2.3 组件设计

> 理解组件的定义、类型和使用方式。

---

## 目录

1. [组件概述](#1-组件概述)
2. [组件类型](#2-组件类型)
3. [组件定义](#3-组件定义)
4. [组件使用](#4-组件使用)

---

## 1. 组件概述

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           组件概述                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   组件 = 贴在实体上的纯数据，不包含任何逻辑                          │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐ │
│   │  组件特点                                                            │ │
│   │                                                                       │ │
│   │  • 纯数据（struct）                                                │ │
│   │  • 无状态（不保存临时计算结果）                                    │ │
│   │  • 易于序列化和复制                                                │ │
│   │  • 可以通过 Matcher 查询                                           │ │
│   └─────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. 组件类型

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           组件类型                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   数据组件                                                                │
│   └── ActorTransformComponent ─── 位置、旋转、速度                       │
│   └── ActorAttributeComponent ─── 生命值、攻击力等                       │
│                                                                             │
│   标记组件                                                                │
│   └── ActorDeadComponent ─── 标记实体已死亡                               │
│   └── ActorInvincibleComponent ─── 标记无敌状态                           │
│                                                                             │
│   缓冲组件                                                                │
│   └── ActorMoveInputComponent ─── 待处理的移动输入                       │
│   └── ActorSkillQueueComponent ─── 技能队列                               │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 3. 组件定义

```csharp
// 组件标记接口
public interface IECComponent { }

// 位置组件
public struct ActorTransformComponent : IECComponent
{
    public readonly int ActorId;
    public readonly Vec3 Position;
    public readonly Vec3 Velocity;
    public readonly float Rotation;

    public ActorTransformComponent(int actorId, Vec3 position, Vec3 velocity, float rotation)
    {
        ActorId = actorId;
        Position = position;
        Velocity = velocity;
        Rotation = rotation;
    }
}

// 属性组件
public struct ActorAttributeComponent : IECComponent
{
    public readonly int ActorId;
    public readonly int MaxHp;
    public readonly int Hp;
    public readonly int Attack;
    public readonly int Defense;

    public ActorAttributeComponent(int actorId, int maxHp, int hp, int attack, int defense)
    {
        ActorId = actorId;
        MaxHp = maxHp;
        Hp = hp;
        Attack = attack;
        Defense = defense;
    }
}
```

---

## 4. 组件使用

```csharp
// 创建实体并添加组件
var entity = world.CreateEntity();
entity.AddComponent<ActorTransformComponent>();
entity.AddComponent<ActorAttributeComponent>();

// 获取组件
var transform = entity.GetComponent<ActorTransformComponent>();
var attribute = entity.GetComponent<ActorAttributeComponent>();

// 检查组件
if (entity.HasComponent<ActorDeadComponent>())
{
    // 实体已死亡
}

// 替换组件
entity.ReplaceComponent(new ActorAttributeComponent(
    attribute.ActorId,
    attribute.MaxHp,
    attribute.Hp - damage,
    attribute.Attack,
    attribute.Defense
));

// 移除组件
entity.RemoveComponent<ActorMoveInputComponent>();
```

---

## 下一步

- [系统设计](./04-SystemDesign.md) - 系统执行机制
- [查询与遍历](../06-ECSArchitecture/03-QueryAndIteration.md) - Matcher 使用

---

*文档版本：v1.0 | 最后更新：2026-06-21*
