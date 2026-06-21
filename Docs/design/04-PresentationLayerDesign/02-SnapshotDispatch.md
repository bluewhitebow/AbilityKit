# 4.2 快照分发

> 理解 FrameSnapshotDispatcher 的设计和实现。

---

## 目录

1. [快照分发概述](#1-快照分发概述)
2. [分发器接口](#2-分发器接口)
3. [快照数据结构](#3-快照数据结构)
4. [订阅机制](#4-订阅机制)

---

## 1. 快照分发概述

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           快照分发概述                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   FrameSnapshotDispatcher = 快照数据分发器                             │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐ │
│   │  职责                                                                  │ │
│   │                                                                       │ │
│   │  • 收集世界状态快照                                                 │ │
│   │  • 分发给订阅者                                                     │ │
│   │  • 支持按需订阅                                                     │ │
│   └─────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. 分发器接口

```csharp
public interface IFrameSnapshotDispatcher
{
    /// <summary>收集快照</summary>
    void CollectSnapshot(int frame, IWorld world);

    /// <summary>分发给订阅者</summary>
    void Dispatch();

    /// <summary>订阅快照</summary>
    void Subscribe(ISnapshotSubscriber subscriber);

    /// <summary>取消订阅</summary>
    void Unsubscribe(ISnapshotSubscriber subscriber);
}

public interface ISnapshotSubscriber
{
    void OnSnapshot(int frame, in FrameSnapshotData snapshot);
}
```

---

## 3. 快照数据结构

```csharp
public readonly struct FrameSnapshotData
{
    public readonly int Frame;
    public readonly int Timestamp;
    public readonly ActorSnapshot[] ActorSnapshots;
    public readonly ProjectileSnapshot[] ProjectileSnapshots;
    public readonly BuffSnapshot[] BuffSnapshots;
}

public readonly struct ActorSnapshot
{
    public readonly int ActorId;
    public readonly Vec3 Position;
    public readonly float Rotation;
    public readonly int Hp;
    public readonly int Mp;
    public readonly ActorState State;
}
```

---

## 4. 订阅机制

```csharp
// 视图层订阅
public sealed class ConsoleBattleView : ISnapshotSubscriber
{
    public void Subscribe(IFrameSnapshotDispatcher dispatcher)
    {
        dispatcher.Subscribe(this);
    }

    public void OnSnapshot(int frame, in FrameSnapshotData snapshot)
    {
        foreach (var actor in snapshot.ActorSnapshots)
        {
            // 渲染实体
            Console.WriteLine($"[{frame}] Actor {actor.ActorId} at ({actor.Position.X}, {actor.Position.Z})");
        }
    }
}
```

---

## 下一步

- [视图事件抽象](./01-ViewEventAbstraction.md) - IBattleViewEventSink
- [跨平台实现](./03-CrossPlatform.md) - Console/Unity/Server 实现

---

*文档版本：v1.0 | 最后更新：2026-06-21*
