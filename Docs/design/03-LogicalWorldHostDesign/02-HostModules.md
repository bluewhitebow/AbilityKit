# 3.2 Host 模块系统

> 理解模块扩展机制的设计和使用。

---

## 目录

1. [模块系统概述](#1-模块系统概述)
2. [模块接口](#2-模块接口)
3. [内置模块](#3-内置模块)
4. [自定义模块](#4-自定义模块)

---

## 1. 模块系统概述

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           模块系统概述                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   模块 = Host 的可插拔扩展单元                                        │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐ │
│   │  模块特点                                                            │ │
│   │                                                                       │ │
│   │  • 独立性强，不影响其他模块                                       │ │
│   │  • 生命周期由 Host 管理                                           │ │
│   │  • 按优先级顺序执行                                               │ │
│   │  • 可动态添加/移除                                               │ │
│   └─────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. 模块接口

```csharp
public interface IHostRuntimeModule
{
    /// <summary>模块名称</summary>
    string Name { get; }

    /// <summary>优先级（越小越先执行）</summary>
    int Priority { get; }

    /// <summary>附加到 Host</summary>
    void OnAttach(IHostRuntime host);

    /// <summary>从 Host 分离</summary>
    void OnDetach();

    /// <summary>每帧 Tick</summary>
    void OnTick(float deltaTime);
}
```

---

## 3. 内置模块

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           内置模块                                         │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   FrameSyncDriverModule (优先级: 1000)                                   │
│   └── 帧同步驱动，处理输入收集和帧推进                                │
│                                                                             │
│   SnapshotProviderModule (优先级: 2000)                                    │
│   └── 快照提供者，生成世界状态快照                                      │
│                                                                             │
│   InputDriverModule (优先级: 500)                                          │
│   └── 输入驱动，处理玩家输入                                             │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. 自定义模块

```csharp
public sealed class MyCustomModule : IHostRuntimeModule
{
    public string Name => "MyCustomModule";
    public int Priority => 3000;

    private IHostRuntime _host;

    public void OnAttach(IHostRuntime host)
    {
        _host = host;
        // 初始化
    }

    public void OnDetach()
    {
        _host = null;
        // 清理
    }

    public void OnTick(float deltaTime)
    {
        // 每帧执行
    }
}

// 使用
var host = WorldHostBuilder.Create()
    .AddModule(new FrameSyncDriverModule())
    .AddModule(new MyCustomModule())
    .Build();
```

---

## 下一步

- [World 管理器](./03-WorldManager.md) - 多世界管理
- [Host 运行时](./01-HostRuntime.md) - Host 核心职责

---

*文档版本：v1.0 | 最后更新：2026-06-21*
