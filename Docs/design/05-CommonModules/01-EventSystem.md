# 5.1 事件系统

> 理解发布-订阅模式在 AbilityKit 中的实现。

---

## 目录

1. [事件系统概述](#1-事件系统概述)
2. [EventDispatcher 实现](#2-eventdispatcher-实现)
3. [订阅与发布](#3-订阅与发布)
4. [使用场景](#4-使用场景)

---

## 1. 事件系统概述

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           事件系统概述                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   发布-订阅模式 = 松耦合的组件通信方式                                  │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐ │
│   │  发布者 (Publisher)                                                    │ │
│   │  • 不知道订阅者是谁                                                 │ │
│   │  • 只负责发布事件                                                  │ │
│   └─────────────────────────────────────────────────────────────────────┘ │
│                                    │                                        │
│                                    ▼                                        │
│   ┌─────────────────────────────────────────────────────────────────────┐ │
│   │  EventDispatcher (事件分发器)                                       │ │
│   │  • 维护订阅者列表                                                  │ │
│   │  • 负责将事件分发给订阅者                                         │ │
│   └─────────────────────────────────────────────────────────────────────┘ │
│                                    │                                        │
│                                    ▼                                        │
│   ┌─────────────────────────────────────────────────────────────────────┐ │
│   │  订阅者 (Subscriber)                                                 │ │
│   │  • 订阅感兴趣的事件                                               │ │
│   │  • 收到事件后执行处理逻辑                                         │ │
│   └─────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. EventDispatcher 实现

```csharp
public interface IEventDispatcher
{
    /// <summary>订阅事件</summary>
    IEventSubscription Subscribe<TArgs>(
        int eventId,
        Action<TArgs> handler,
        int priority = 0,
        bool once = false);

    /// <summary>发布事件</summary>
    void Publish<TArgs>(int eventId, in TArgs args) where TArgs : struct;

    /// <summary>取消订阅</summary>
    void Unsubscribe(int eventId, IEventSubscription subscription);
}
```

---

## 3. 订阅与发布

```csharp
// 订阅
var subscription = dispatcher.Subscribe<DamageEvent>(
    eventId: GameEvents.OnDamageDealt,
    handler: OnDamage,
    priority: 100
);

// 发布
dispatcher.Publish(GameEvents.OnDamageDealt, new DamageEvent
{
    AttackerId = 1,
    TargetId = 2,
    Damage = 100
});

// 取消订阅
subscription.Dispose();
```

---

## 4. 使用场景

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           使用场景                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   视图更新                                                                │
│   SkillSystem ──▶ 发布 OnDamage ──▶ ViewSystem 订阅 ──▶ 渲染伤害数字  │
│                                                                             │
│   系统间通信                                                              │
│   SkillExecutor ──▶ 发布 OnSkillCast ──▶ BuffSystem 订阅 ──▶ 检查触发  │
│                                                                             │
│   跨模块通信                                                              │
│   DamageSystem ──▶ 发布 OnKill ──▶ AchievementSystem 订阅 ──▶ 解锁成就  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 下一步

- [对象池](./02-ObjectPool.md) - 性能优化

---

*文档版本：v1.0 | 最后更新：2026-06-21*
