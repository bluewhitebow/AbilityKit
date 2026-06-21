# 9.1 Console Demo 解析

> 通过 Console Demo 理解框架的使用方式。

---

## 目录

1. [Demo 概述](#1-demo-概述)
2. [项目结构](#2-项目结构)
3. [启动流程](#3-启动流程)
4. [帧循环解析](#4-帧循环解析)

---

## 1. Demo 概述

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           Console Demo 概述                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   Console Demo = AbilityKit 的无 Unity 验证环境                         │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐ │
│   │  设计目标                                                            │ │
│   │  • 快速验证帧同步逻辑                                               │ │
│   │  • 跨平台验证（无需 Unity）                                         │ │
│   │  • 易于调试和复现问题                                               │ │
│   │  • 完整的帧同步 + 回放能力                                          │ │
│   └─────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. 项目结构

```
src/AbilityKit.Demo.Moba.Console/
├── Configs/moba/          # 游戏配置
│   ├── characters.json     # 角色配置
│   └── skills.json        # 技能配置
├── Platform/              # 平台抽象
│   └── Console/           # Console 平台实现
├── Services/              # 业务服务
│   └── Battle/            # 战斗相关
├── Flow/                  # 流程控制
└── Bootstrap/             # 启动器
```

---

## 3. 启动流程

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           启动流程                                         │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   1. ConsoleBattleBootstrapper.Run()                                      │
│                                                                             │
│   2. 创建 HostRuntime                                                     │
│      var host = WorldHostBuilder.Create()                                  │
│          .SetWorldFactory(factory)                                        │
│          .AddModule(new FrameSyncDriverModule())                          │
│          .Build();                                                        │
│                                                                             │
│   3. 创建 World                                                           │
│      host.CreateWorld(options);                                           │
│                                                                             │
│   4. 启动帧循环                                                          │
│      while (running)                                                      │
│          host.Tick(deltaTime);                                            │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. 帧循环解析

```csharp
public void Tick(float deltaTime)
{
    // 1. 等待输入
    _syncAdapter.WaitForInputs();

    // 2. 执行逻辑帧
    _syncAdapter.ProcessLogicFrame(deltaTime);

    // 3. 发布快照
    _snapshotDispatcher.Publish(_currentFrame, _snapshot);

    // 4. 推进帧号
    _currentFrame++;
}
```

---

## 下一步

- [ET Demo 解析](../02-ETDemoAnalysis.md) - ET Framework 集成示例

---

*文档版本：v1.0 | 最后更新：2026-06-21*
