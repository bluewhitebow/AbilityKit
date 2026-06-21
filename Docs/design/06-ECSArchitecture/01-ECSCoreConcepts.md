# 6.1 ECS 核心概念

> 理解 Entity、Component、System 在 AbilityKit 中的实现。

---

## 目录

1. [ECS 核心定义](#1-ecs-核心定义)
2. [Entity 实体](#2-entity-实体)
3. [Component 组件](#3-component-组件)
4. [System 系统](#4-system-系统)

---

## 1. ECS 核心定义

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           ECS 核心定义                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   ECS = Entity + Component + System                                       │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐ │
│   │  Entity (实体)                                                          │ │
│   │  • 唯一 ID，不包含任何数据                                           │ │
│   │  • 只是一个"钩子"，用于关联组件                                     │ │
│   └─────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐ │
│   │  Component (组件)                                                      │ │
│   │  • 纯数据，不包含任何逻辑                                           │ │
│   │  • 贴在实体上，为实体提供数据                                       │ │
│   └─────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐ │
│   │  System (系统)                                                        │ │
│   │  • 纯逻辑，不包含任何状态                                           │ │
│   │  • 处理特定类型的组件                                               │ │
│   └─────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Entity 实体

```csharp
public interface IECEntity
{
    /// <summary>实体 ID</summary>
    int Id { get; }

    /// <summary>是否已销毁</summary>
    bool IsDestroyed { get; }

    // ========== 组件操作 ==========

    /// <summary>添加组件</summary>
    void AddComponent<T>() where T : struct, IECComponent;

    /// <summary>获取组件</summary>
    T GetComponent<T>() where T : struct, IECComponent;

    /// <summary>是否有组件</summary>
    bool HasComponent<T>() where T : struct, IECComponent;

    /// <summary>移除组件</summary>
    void RemoveComponent<T>() where T : struct, IECComponent;

    /// <summary>替换组件</summary>
    void ReplaceComponent<T>(T component) where T : struct, IECComponent;
}
```

---

## 3. Component 组件

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

## 4. System 系统

```csharp
public interface IECSystem
{
    /// <summary>系统名称</summary>
    string Name { get; }

    /// <summary>优先级</summary>
    int Priority { get; }

    /// <summary>是否启用</summary>
    bool IsEnabled { get; set; }

    /// <summary>初始化</summary>
    void Initialize(IWorld world);

    /// <summary>执行</summary>
    void Execute();

    /// <summary>销毁</summary>
    void Destroy();
}

// 示例：移动系统
public sealed class MobaMotionSystem : IECSystem
{
    private IWorld _world;

    public string Name => "MobaMotionSystem";
    public int Priority => 1000;
    public bool IsEnabled { get; set; } = true;

    public void Initialize(IWorld world) => _world = world;

    public void Execute()
    {
        var entities = _world.GetEntities(
            Matcher.AllOf<ActorTransformComponent, ActorMoveInputComponent>()
        );

        foreach (var entity in entities)
        {
            // 处理移动逻辑
        }
    }

    public void Destroy() { }
}
```

---

## 下一步

- [Entitas 实现](../02-EntitasImplementation.md) - 框架 ECS 实现详解

---

*文档版本：v1.0 | 最后更新：2026-06-21*
