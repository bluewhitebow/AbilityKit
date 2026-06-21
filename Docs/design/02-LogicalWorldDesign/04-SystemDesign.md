# 2.4 系统设计

> 理解系统的组织、优先级和执行方式。

---

## 目录

1. [系统概述](#1-系统概述)
2. [系统接口](#2-系统接口)
3. [系统优先级](#3-系统优先级)
4. [系统类型](#4-系统类型)

---

## 1. 系统概述

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           系统概述                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   系统 = 处理特定组件的纯逻辑，不包含任何状态                      │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐ │
│   │  系统特点                                                            │ │
│   │                                                                       │ │
│   │  • 纯逻辑（无状态）                                                │ │
│   │  • 通过 Matcher 查询需要处理的实体                                 │ │
│   │  • 按优先级顺序执行                                                │ │
│   │  • 可以启用/禁用                                                   │ │
│   └─────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. 系统接口

```csharp
public interface IECSystem
{
    /// <summary>系统名称</summary>
    string Name { get; }

    /// <summary>优先级（越小越先执行）</summary>
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
```

---

## 3. 系统优先级

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           系统优先级                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   优先级数字越小，越先执行                                           │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐ │
│   │  典型优先级配置                                                     │ │
│   │                                                                       │ │
│   │  1000 - 输入处理系统                                              │ │
│   │  2000 - 移动系统                                                  │ │
│   │  3000 - 技能系统                                                  │ │
│   │  4000 - 伤害系统                                                  │ │
│   │  5000 - Buff 系统                                                 │ │
│   │  6000 - 死亡系统                                                  │ │
│   │  7000 - 视图更新系统                                              │ │
│   └─────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. 系统类型

```csharp
// 初始化系统（游戏开始时执行一次）
public interface IECInitializeSystem : IECSystem
{
    void Initialize();
}

// 每帧执行系统
public interface IExecuteSystem : IECSystem
{
    void Execute();
}

// 定时执行系统
public interface ITickSystem : IECSystem
{
    void Tick(float deltaTime);
}

// 清理系统（游戏结束时执行一次）
public interface ICleanupSystem : IECSystem
{
    void Cleanup();
}

// 示例：移动系统
public sealed class MobaMotionSystem : IECSystem
{
    private IWorld _world;

    public string Name => "MobaMotionSystem";
    public int Priority => 2000;
    public bool IsEnabled { get; set; } = true;

    public void Initialize(IWorld world) => _world = world;

    public void Execute()
    {
        var entities = _world.GetEntities(
            Matcher.AllOf<ActorTransformComponent, ActorMoveInputComponent>(),
            Matcher.NoneOf<ActorDeadComponent>()
        );

        foreach (var entity in entities)
        {
            // 处理移动逻辑
            var transform = entity.GetComponent<ActorTransformComponent>();
            var moveInput = entity.GetComponent<ActorMoveInputComponent>();

            var direction = (moveInput.TargetPosition - transform.Position).Normalize();
            var newPosition = transform.Position + direction * moveInput.Speed * Time.deltaTime;

            entity.ReplaceComponent(new ActorTransformComponent(
                transform.ActorId,
                newPosition,
                direction * moveInput.Speed,
                transform.Rotation
            ));
        }
    }

    public void Destroy() { }
}
```

---

## 下一步

- [World 概述](./01-WorldOverview.md) - World 核心职责
- [查询与遍历](../06-ECSArchitecture/03-QueryAndIteration.md) - Matcher 使用

---

*文档版本：v1.0 | 最后更新：2026-06-21*
