# AbilityKit 框架设计文档

> AbilityKit 是一款面向 MOBA 类游戏的高性能技能系统框架。本文档将帮助你从零开始理解框架的设计理念、架构决策和最佳实践。

---

## 文档导航

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        AbilityKit 设计文档体系                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  📚 阅读路线图                                                             │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐ │
│  │  1. 概览入门                                                         │ │
│  │     了解 AbilityKit 是什么，能解决什么问题                          │ │
│  └─────────────────────────────────────────────────────────────────────┘ │
│                                     │                                        │
│                                     ▼                                        │
│  ┌─────────────────────────────────────────────────────────────────────┐ │
│  │  2. 逻辑世界设计                                                      │ │
│  │     理解框架如何组织和管理游戏逻辑数据                               │ │
│  └─────────────────────────────────────────────────────────────────────┘ │
│                                     │                                        │
│                                     ▼                                        │
│  ┌─────────────────────────────────────────────────────────────────────┐ │
│  │  3. 逻辑世界 Host 设计                                               │ │
│  │     掌握逻辑世界的运行时环境和生命周期管理                           │ │
│  └─────────────────────────────────────────────────────────────────────┘ │
│                                     │                                        │
│                                     ▼                                        │
│  ┌─────────────────────────────────────────────────────────────────────┐ │
│  │  4. 表现层设计                                                       │ │
│  │     学习如何将逻辑层与表现层解耦                                     │ │
│  └─────────────────────────────────────────────────────────────────────┘ │
│                                     │                                        │
│                                     ▼                                        │
│  ┌─────────────────────────────────────────────────────────────────────┐ │
│  │  5. 通用模块                                                         │ │
│  │     深入框架提供的核心能力模块                                       │ │
│  └─────────────────────────────────────────────────────────────────────┘ │
│                                     │                                        │
│                                     ▼                                        │
│  ┌─────────────────────────────────────────────────────────────────────┐ │
│  │  6. ECS 架构                                                         │ │
│  │     理解 ECS 在框架中的实现和应用                                   │ │
│  └─────────────────────────────────────────────────────────────────────┘ │
│                                     │                                        │
│                                     ▼                                        │
│  ┌─────────────────────────────────────────────────────────────────────┐ │
│  │  7. 网络同步                                                         │ │
│  │     掌握帧同步、状态同步和回放系统的实现                            │ │
│  └─────────────────────────────────────────────────────────────────────┘ │
│                                     │                                        │
│                                     ▼                                        │
│  ┌─────────────────────────────────────────────────────────────────────┐ │
│  │  8. 玩法模块                                                         │ │
│  │     技能、触发器、Buff、投射物等玩法系统的设计                     │ │
│  └─────────────────────────────────────────────────────────────────────┘ │
│                                     │                                        │
│                                     ▼                                        │
│  ┌─────────────────────────────────────────────────────────────────────┐ │
│  │  9. 实现示例                                                         │ │
│  │     通过实际代码理解框架的使用方式                                   │ │
│  └─────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 文档目录

### 01 概览与入门

| 文档 | 说明 | 优先级 |
|------|------|--------|
| [01-AbilityKit 是什么](01-OverviewAndGettingStarted/01-WhatIsAbilityKit.md) | 框架定位、核心能力、解决问题 | ⭐⭐⭐ |
| [02-核心概念](01-OverviewAndGettingStarted/02-CoreConcepts.md) | 框架术语体系 | ⭐⭐⭐ |
| [03-快速开始](01-OverviewAndGettingStarted/03-QuickStart.md) | 30 分钟运行第一个 Demo | ⭐⭐ |
| [04-项目结构](01-OverviewAndGettingStarted/04-ProjectStructure.md) | 目录结构与包依赖 | ⭐⭐ |

---

### 02 逻辑世界设计

| 文档 | 说明 | 优先级 |
|------|------|--------|
| [01-逻辑世界概述](02-LogicalWorldDesign/01-WorldOverview.md) | IWorld 接口与职责 | ⭐⭐⭐ |
| [02-实体设计](02-LogicalWorldDesign/02-EntityDesign.md) | 实体的创建与销毁 | ⭐⭐⭐ |
| [03-组件设计](02-LogicalWorldDesign/03-ComponentDesign.md) | 组件的定义与使用 | ⭐⭐⭐ |
| [04-系统设计](02-LogicalWorldDesign/04-SystemDesign.md) | 系统的组织与执行 | ⭐⭐⭐ |
| [05-服务容器](02-LogicalWorldDesign/05-ServiceContainer.md) | 依赖注入与生命周期 | ⭐⭐⭐ |

---

### 03 逻辑世界 Host 设计

| 文档 | 说明 | 优先级 |
|------|------|--------|
| [01-Host 运行时](03-LogicalWorldHostDesign/01-HostRuntime.md) | HostRuntime 职责 | ⭐⭐⭐ |
| [02-Host 模块系统](03-LogicalWorldHostDesign/02-HostModules.md) | 模块扩展机制 | ⭐⭐ |
| [03-World 管理器](03-LogicalWorldHostDesign/03-WorldManager.md) | 多世界管理 | ⭐⭐ |

---

### 04 表现层设计

| 文档 | 说明 | 优先级 |
|------|------|--------|
| [01-视图事件抽象](04-PresentationLayerDesign/01-ViewEventAbstraction.md) | IBattleViewEventSink | ⭐⭐⭐ |
| [02-快照分发](04-PresentationLayerDesign/02-SnapshotDispatch.md) | FrameSnapshotDispatcher | ⭐⭐⭐ |
| [03-跨平台实现](04-PresentationLayerDesign/03-CrossPlatform.md) | Console/Unity/Server 平台 | ⭐⭐ |

---

### 05 通用模块

| 文档 | 说明 | 优先级 |
|------|------|--------|
| [01-事件系统](05-CommonModules/01-EventSystem.md) | 发布订阅模式实现 | ⭐⭐⭐ |
| [02-对象池](05-CommonModules/02-ObjectPool.md) | 性能优化 | ⭐⭐ |
| [03-定时器框架](05-CommonModules/03-TimerFramework.md) | 时间管理 | ⭐⭐ |
| [04-配置系统](05-CommonModules/04-ConfigurationSystem.md) | 配置加载与验证 | ⭐⭐ |

---

### 06 ECS 架构

| 文档 | 说明 | 优先级 |
|------|------|--------|
| [01-ECS 核心概念](06-ECSArchitecture/01-ECSCoreConcepts.md) | Entity/Component/System | ⭐⭐⭐ |
| [02-Entitas 实现](06-ECSArchitecture/02-EntitasImplementation.md) | 框架 ECS 实现 | ⭐⭐ |
| [03-查询与遍历](06-ECSArchitecture/03-QueryAndIteration.md) | Matcher 与批量处理 | ⭐⭐ |

---

### 07 网络同步

| 文档 | 说明 | 优先级 |
|------|------|--------|
| [01-帧同步机制](07-NetworkSynchronization/01-FrameSync.md) | Lockstep 实现 | ⭐⭐⭐ |
| [02-状态同步](07-NetworkSynchronization/02-StateSync.md) | 快照权威模式 | ⭐⭐ |
| [03-回滚预测](07-NetworkSynchronization/03-RollbackPrediction.md) | 客户端预测与校验 | ⭐⭐ |
| [04-回放系统](07-NetworkSynchronization/04-ReplaySystem.md) | 录制与回放 | ⭐⭐ |
| [05-会话协调](07-NetworkSynchronization/05-SessionCoordination.md) | SessionCoordinator | ⭐⭐ |

---

### 08 玩法模块

| 文档 | 说明 | 优先级 |
|------|------|--------|
| [01-技能系统架构](08-GameplayModules/01-SkillSystemArchitecture.md) | 技能配置与执行管线 | ⭐⭐⭐ |
| [02-触发器系统](08-GameplayModules/02-TriggeringSystem.md) | 条件-动作机制 | ⭐⭐⭐ |
| [03-Buff 系统](08-GameplayModules/03-BuffSystem.md) | Buff 生命周期管理 | ⭐⭐⭐ |
| [04-投射物系统](08-GameplayModules/04-ProjectileSystem.md) | 投射物飞行与命中 | ⭐⭐ |
| [05-属性系统](08-GameplayModules/05-AttributeSystem.md) | Attributes 与 Modifiers | ⭐⭐⭐ |
| [06-伤害计算](08-GameplayModules/06-DamageCalculation.md) | 伤害公式与护甲减伤 | ⭐⭐ |

---

### 09 实现示例

| 文档 | 说明 | 优先级 |
|------|------|--------|
| [01-Console Demo 解析](09-ImplementationExamples/01-ConsoleDemoAnalysis.md) | Console Demo 架构解析 | ⭐⭐ |
| [02-ET Demo 解析](09-ImplementationExamples/02-ETDemoAnalysis.md) | ET Demo 架构解析 | ⭐⭐ |

---

## 快速索引

### 按主题查找

| 你想了解... | 去这里 |
|-----------|-------|
| 什么是 AbilityKit | [01-AbilityKit 是什么](01-OverviewAndGettingStarted/01-WhatIsAbilityKit.md) |
| 如何快速运行 Demo | [03-快速开始](01-OverviewAndGettingStarted/03-QuickStart.md) |
| 逻辑世界是什么 | [02-逻辑世界设计](02-LogicalWorldDesign) |
| 如何添加新系统 | [02-系统设计](02-LogicalWorldDesign/04-SystemDesign.md) |
| 如何实现网络同步 | [07-网络同步](07-NetworkSynchronization) |
| 技能是如何执行的 | [01-技能系统架构](08-GameplayModules/01-SkillSystemArchitecture.md) |
| 如何添加新 Buff | [03-Buff 系统](08-GameplayModules/03-BuffSystem.md) |
| 如何添加新触发器 | [02-触发器系统](08-GameplayModules/02-TriggeringSystem.md) |

---

## 文档更新记录

| 日期 | 版本 | 更新内容 |
|------|------|---------|
| 2026-06-20 | 1.0 | 初始版本，建立文档框架 |
| 2026-06-21 | 1.1 | 按功能模块重新组织目录结构 |

---

*本文档将持续更新完善*
