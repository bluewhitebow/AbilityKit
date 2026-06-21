# 4.3 跨平台实现

> 理解 Console、Unity、Server 三种平台的表现层实现。

---

## 目录

1. [跨平台概述](#1-跨平台概述)
2. [Console 平台](#2-console-平台)
3. [Unity 平台](#3-unity-平台)
4. [Server 平台](#4-server-平台)

---

## 1. 跨平台概述

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           跨平台实现概述                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   同一套逻辑层，不同平台有不同表现层实现                            │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐ │
│   │  Console 平台                                                         │ │
│   │  └── ASCII 字符渲染，用于无 Unity 环境验证                         │ │
│   └─────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐ │
│   │  Unity 平台                                                           │ │
│   │  └── GameObject 渲染，用于实际游戏运行                              │ │
│   └─────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐ │
│   │  Server 平台                                                         │ │
│   │  └── 仅数据处理，不渲染，用于服务器权威                            │ │
│   └─────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Console 平台

```csharp
public sealed class ConsoleBattleView : IBattleViewEventSink
{
    public void OnActorSpawn(in ActorSpawnData spawn)
    {
        Console.WriteLine($"[Spawn] {spawn.Name} (ID:{spawn.ActorId}) at ({spawn.Position.X}, {spawn.Position.Z})");
    }

    public void OnActorMove(in ActorMoveData move)
    {
        Console.WriteLine($"[Move] Actor {move.ActorId} -> ({move.Position.X}, {move.Position.Z})");
    }

    public void OnActorDamage(in ActorDamageData damage)
    {
        Console.WriteLine($"[Damage] Actor {damage.TargetId} took {damage.Damage} damage, HP: {damage.NewHp}");
    }

    public void OnActorDead(in ActorDeadData dead)
    {
        Console.WriteLine($"[Dead] Actor {dead.ActorId} killed by {dead.KillerId}");
    }
}
```

---

## 3. Unity 平台

```csharp
public sealed class UnityBattleView : IBattleViewEventSink
{
    private readonly Dictionary<int, GameObject> _actorViews = new();

    public void OnActorSpawn(in ActorSpawnData spawn)
    {
        var prefab = Resources.Load<GameObject>($"Actors/{spawn.HeroId}");
        var go = Object.Instantiate(prefab, spawn.Position.ToVector3(), Quaternion.Euler(0, spawn.Rotation, 0));
        _actorViews[spawn.ActorId] = go;
    }

    public void OnActorMove(in ActorMoveData move)
    {
        if (_actorViews.TryGetValue(move.ActorId, out var go))
        {
            go.transform.position = move.Position.ToVector3();
        }
    }

    public void OnActorDamage(in ActorDamageData damage)
    {
        // 显示伤害数字
        ShowDamageNumber(damage.TargetId, damage.Damage);
    }

    public void OnActorDead(in ActorDeadData dead)
    {
        if (_actorViews.TryGetValue(dead.ActorId, out var go))
        {
            // 播放死亡动画
            // 延迟销毁
            Object.Destroy(go, 2f);
            _actorViews.Remove(dead.ActorId);
        }
    }
}
```

---

## 4. Server 平台

```csharp
public sealed class ServerBattleView : IBattleViewEventSink
{
    // 服务器端不需要渲染，只需记录事件用于日志或回放

    public void OnActorSpawn(in ActorSpawnData spawn)
    {
        // 记录日志
        _logger.Log($"[Server] Actor {spawn.ActorId} spawned");
    }

    public void OnActorMove(in ActorMoveData move)
    {
        // 服务器不记录移动日志
    }

    public void OnActorDamage(in ActorDamageData damage)
    {
        // 记录伤害日志
        _logger.Log($"[Server] Damage: {damage.Damage} to {damage.TargetId}, HP: {damage.NewHp}");
    }

    public void OnActorDead(in ActorDeadData dead)
    {
        // 记录死亡日志
        _logger.Log($"[Server] Actor {dead.ActorId} killed by {dead.KillerId}");
    }
}
```

---

## 下一步

- [视图事件抽象](./01-ViewEventAbstraction.md) - IBattleViewEventSink
- [快照分发](./02-SnapshotDispatch.md) - FrameSnapshotDispatcher

---

*文档版本：v1.0 | 最后更新：2026-06-21*
