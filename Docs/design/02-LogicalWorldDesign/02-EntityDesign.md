# 2.2 实体设计

> 理解实体的创建、销毁和管理机制。

---

## 目录

1. [实体概述](#1-实体概述)
2. [实体接口](#2-实体接口)
3. [实体生命周期](#3-实体生命周期)
4. [实体管理](#4-实体管理)

---

## 1. 实体概述

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           实体概述                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   实体 = 唯一标识符，不包含任何数据                                  │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐ │
│   │  实体的作用                                                          │ │
│   │                                                                       │ │
│   │  • 作为组件的"容器"                                               │ │
│   │  • 提供唯一 ID 用于索引                                           │ │
│   │  • 不包含任何数据或逻辑                                           │ │
│   └─────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. 实体接口

```csharp
public interface IECEntity
{
    /// <summary>实体 ID</summary>
    int Id { get; }

    /// <summary>是否已销毁</summary>
    bool IsDestroyed { get; }

    // ========== 组件操作 ==========

    void AddComponent<T>() where T : struct, IECComponent;
    T GetComponent<T>() where T : struct, IECComponent;
    bool HasComponent<T>() where T : struct, IECComponent;
    void RemoveComponent<T>() where T : struct, IECComponent;
    void ReplaceComponent<T>(T component) where T : struct, IECComponent;
}
```

---

## 3. 实体生命周期

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           实体生命周期                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   ┌──────────┐     CreateEntity    ┌──────────┐     DestroyEntity   ┌──────────┐
│   │  Pooled   │ ──────────────────▶│  Active   │ ──────────────────▶│  Destroyed│
│   └──────────┘                    └──────────┘                     └──────────┘
│                                                                             │
│   状态说明：                                                               │
│   ├── Pooled:   对象池中，未使用                                        │
│   ├── Active:   正在使用，拥有组件                                       │
│   └── Destroyed: 已销毁，组件已清空                                      │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. 实体管理

```csharp
public sealed class GameContext : IWorld
{
    private readonly Dictionary<int, IECEntity> _entities = new();
    private int _nextEntityId = 1;

    public IECEntity CreateEntity()
    {
        var entity = new ActorEntity(_nextEntityId++);
        _entities[entity.Id] = entity;
        return entity;
    }

    public void DestroyEntity(IECEntity entity)
    {
        // 移除所有组件
        // 从字典中移除
        _entities.Remove(entity.Id);
    }
}
```

---

## 下一步

- [组件设计](./03-ComponentDesign.md) - 深入理解组件
- [系统设计](./04-SystemDesign.md) - 系统执行机制

---

*文档版本：v1.0 | 最后更新：2026-06-21*
