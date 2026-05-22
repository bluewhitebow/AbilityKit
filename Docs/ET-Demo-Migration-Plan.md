# ET Demo 迁移计划

## 概述

基于 Moba.Console 的架构，将 ET Demo 重构为完整的技能战斗框架示例。

## 当前状态

### Moba.Console 核心模块

| 模块 | 状态 | 说明 |
|------|------|------|
| Feature 系统 | ✅ 完整 | FeatureHost, ModuleHost |
| 战斗上下文 | ✅ 完整 | ConsoleBattleContext |
| ECS 实体 | ✅ 完整 | BattleEntityFactory, Query |
| 输入系统 | ✅ 完整 | ConsoleInputFeature |
| 帧同步 | ✅ 完整 | ConsoleSyncFeature |
| 视图层 | ✅ 完整 | ConsoleViewFeature + Modules |
| 会话管理 | ✅ 完整 | SessionOrchestrator |
| 快照系统 | ✅ 完整 | SnapshotPipeline |
| HUD | ✅ 完整 | ConsoleHudFeature |
| 回放 | ⚠️ 基础 | ReplayRecorder |

### ET Demo 当前状态

| 模块 | 状态 | 说明 |
|------|------|------|
| Component/System | ✅ 完整 | ETBattleComponent, ETUnitComponent, ETUnit 等 |
| 视图层 | ✅ 完整 | ETBattleViewComponent, ETUnitViewComponent |
| 事件系统 | ✅ 完整 | IETViewEventSink, 所有事件定义 |
| 输入 | ✅ 完整 | ETInputComponent + ETInputComponentSystem |
| 帧同步 | ✅ 基础 | ETSessionComponent + ETSessionComponentSystem |
| 流程管理 | ✅ 完整 | ETFlowComponent + ETFlowComponentSystem |
| 会话管理 | ✅ 完整 | ETSessionComponent |
| 入口整合 | ✅ 完整 | DemoBattleEntry 整合战斗流程 |
| 视图绑定 | ✅ 完整 | ETViewEventSink + ETViewEventHandler |

---

## 迁移阶段

### 阶段 0: 清理旧代码

**目标**: 清理当前简化代码，准备新架构

**任务**:
- [ ] 删除 `DemoBattleComponent.cs` 中的逻辑（迁移到 System）
- [ ] 删除 `DemoLoginComponent.cs` 中的逻辑（迁移到 System）
- [ ] 删除 `DemoProcessComponent.cs` 中的逻辑（迁移到 System）
- [ ] 清理 `ETDemoUnitViewComponent.cs`
- [ ] 清理 `DemoEventHandlers.cs`

**文件变更**:
```
待清理文件:
├── Logic/Model/Battle/DemoBattleComponent.cs  (只保留数据)
├── Logic/Model/Login/DemoLoginComponent.cs   (只保留数据)
├── Logic/Model/Process/DemoProcessComponent.cs (只保留数据)
├── Hotfix/Share/DemoBattleComponentSystem.cs  (待重构)
├── Hotfix/Share/DemoUnitComponentSystem.cs   (待重构)
├── Hotfix/Share/DemoUnitSystem.cs           (待重构)
├── Hotfix/Share/DemoEventHandlers.cs        (待重构)
├── View/ModelView/Client/Unit/ETDemoUnitViewComponent.cs (待清理)
└── HotfixView/...                           (待清理)
```

---

### 阶段 1: 核心接口迁移

**目标**: 迁移 Moba.Console 的核心接口定义到 ET Demo

**任务**:

#### 1.1 事件接口
- [x] 在 Share 层定义 `IETViewEventSink`
- [x] 在 Share 层定义 `IETInputSink`
- [x] 定义单位相关事件（创建、销毁、移动、伤害等）

#### 1.2 核心数据结构
- [x] 定义 `BattleStartPlan` - 战斗启动计划
- [x] 定义 `BattleState` - 战斗状态枚举
- [x] 定义 `ActorData` - Actor 数据
- [x] 定义 `SkillData` - 技能数据

**新增文件**:
```
Share/
├── Interface/
│   ├── IETViewEventSink.cs      # 视图事件接口
│   ├── IETInputSink.cs          # 输入接口
│   └── IETBattleContextSink.cs   # 战斗上下文接口
├── Model/
│   ├── Battle/
│   │   ├── BattleStartPlan.cs
│   │   ├── BattleState.cs
│   │   └── ActorData.cs
│   └── Events/
│       ├── Actor/
│       │   ├── ActorSpawnEvent.cs
│       │   ├── ActorDeadEvent.cs
│       │   ├── ActorMoveEvent.cs
│       │   └── ActorDamageEvent.cs
│       └── Battle/
│           ├── BattleStartEvent.cs
│           ├── BattleEndEvent.cs
│           └── FrameTickEvent.cs
```

---

### 阶段 2: 战斗系统迁移

**目标**: 实现 ET 版本的战斗系统

**任务**:

#### 2.1 Component 定义
- [x] `ETBattleComponent` - 战斗管理器
- [x] `ETUnitComponent` - 单位管理器
- [x] `ETUnit` - 单位实体
- [x] `ETSessionComponent` - 会话组件

#### 2.2 System 实现
- [x] `ETBattleComponentSystem`
- [x] `ETUnitComponentSystem`
- [x] `ETUnitSystem`
- [x] `ETSessionComponentSystem`

#### 2.3 流程管理
- [x] `ETFlowComponent` - 流程组件
- [x] `ETFlowComponentSystem` - 流程 System

**文件结构**:
```
Logic/
├── Model/
│   └── Battle/
│       ├── ETBattleComponent.cs      # 战斗管理器
│       ├── ETUnitComponent.cs       # 单位管理器
│       ├── ETUnit.cs                # 单位实体
│       ├── ETSessionComponent.cs    # 会话组件
│       ├── ETBattleFlowComponent.cs # 流程组件
│       └── BattleState.cs           # 状态枚举
└── Hotfix/Share/
    ├── Battle/
    │   ├── ETBattleComponentSystem.cs
    │   ├── ETUnitComponentSystem.cs
    │   ├── ETUnitSystem.cs
    │   ├── ETSessionComponentSystem.cs
    │   └── ETBattleFlowSystem.cs
    └── Events/
        └── BattleEventHandlers.cs
```

---

### 阶段 3: 视图层迁移

**目标**: 实现 ET 版本的视图层

**任务**:

#### 3.1 视图 Component
- [x] `ETBattleViewComponent` - 视图管理器
- [x] `ETUnitViewComponent` - 单位视图组件

#### 3.2 视图 System
- [x] `ETBattleViewComponentSystem`
- [x] `ETUnitViewComponentSystem`

#### 3.3 视图事件订阅
- [x] 实现 `IETViewEventSink` 订阅
- [x] 实现单位创建/销毁/移动事件处理

**文件结构**:
```
View/
├── ModelView/Client/
│   ├── ETBattleViewComponent.cs   # 视图管理器
│   └── Unit/
│       └── ETUnitViewComponent.cs # 单位视图
├── ModelView/Client/Battle/
│   └── ETBattleViewComponentSystem.cs
└── ModelView/Client/Unit/
    └── ETUnitViewComponentSystem.cs
```

---

### 阶段 4: 输入系统迁移

**目标**: 实现 ET 版本的输入系统

**任务**:
- [x] `ETInputComponent` - 输入组件
- [x] `ETInputComponentSystem` - 输入 System
- [x] 实现 `IETInputSink`

**文件结构**:
```
Logic/
├── Model/Input/
│   └── ETInputComponent.cs
└── Hotfix/Share/Input/
    └── ETInputComponentSystem.cs
```

---

### 阶段 5: 帧同步系统 (可选)

**目标**: 实现帧同步（如果需要）

**任务**:
- [x] `ETSessionComponent` - 帧同步组件 (基础)
- [x] `ETSessionComponentSystem` - 帧同步 System (基础)
- [ ] 帧命令定义 (可选)
- [ ] 回放系统 (可选)

---

## 接口设计

### IETViewEventSink

```csharp
public interface IETViewEventSink
{
    // 单位事件
    void OnActorSpawn(long actorId, string name, float x, float y, float hp, float maxHp);
    void OnActorDead(long actorId);
    void OnActorMove(long actorId, float x, float y);
    void OnActorDamage(long actorId, float damage, float hpAfter, float maxHp);
    void OnActorHealthChange(long actorId, float hp, float maxHp);

    // 技能事件
    void OnSkillCast(long casterId, int skillId, float targetX, float targetY);
    void OnSkillHit(long targetId, int skillId, float damage);

    // 特效事件
    void OnVfxSpawn(long actorId, string vfxId, float x, float y);
    void OnFloatingText(long actorId, string text, float x, float y);
}
```

### IETInputSink

```csharp
public interface IETInputSink
{
    void SubmitMoveInput(long actorId, float x, float y);
    void SubmitSkillInput(long actorId, int skillSlot, float targetX, float targetY);
    void SubmitStopInput(long actorId);
}
```

---

## 迁移顺序

```
阶段 0 (清理)
    ↓
阶段 1 (核心接口)
    ↓
阶段 2 (战斗系统)
    ↓
阶段 3 (视图层)
    ↓
阶段 4 (输入系统)
    ↓
阶段 5 (帧同步 - 可选)
```

---

## 命名规范

### ET Demo 命名

| 类型 | 命名 | 示例 |
|------|------|------|
| Component | `ET*Component` | `ETBattleComponent` |
| System | `ET*ComponentSystem` | `ETBattleComponentSystem` |
| Entity | `ET*` | `ETUnit` |
| Event | `*Event` | `BattleStartEvent` |
| Handler | `*Handler` | `BattleStartHandler` |
| Interface | `I*Sink` | `IETViewEventSink` |

### 对应 Moba.Console

| Moba.Console | ET Demo |
|--------------|---------|
| `ConsoleBattleView` | `ETBattleViewComponent` |
| `ConsoleBattleContext` | `ETBattleComponent + ETUnitComponent` |
| `BattleSession` | `ETSessionComponent` |
| `BattleEntity` | `ETUnit` |
| `IViewEventSink` | `IETViewEventSink` |
| `IInputFeature` | `IETInputSink` |

---

## 注意事项

1. **ET 特性**: 使用 Component + System 模式
2. **事件驱动**: 通过 ET 事件系统解耦
3. **跨平台**: 不依赖 Unity 特定 API
4. **可测试**: 逻辑在 System 中，易于单元测试
