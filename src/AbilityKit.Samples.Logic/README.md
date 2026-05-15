# AbilityKit.Samples.Logic

> 示例代码 — 展示 Ability-Kit 各模块的使用方式

## 目录结构

```
AbilityKit.Samples.Logic/
├── Ability/                        # 技能系统示例
│   ├── Core/                      # 核心接口（IPipeline, IAction, ActionContext 等）
│   │   ├── Action/                # IAction 接口、IActionFactory、ActionRegistry
│   │   ├── Bootstrap/             # WorldBlueprint、BootstrapPipeline、IWorldModule
│   │   ├── Component/             # ComponentContext、IComponentSystem
│   │   └── Pipeline/              # IPipeline、IPipelinePhase、PipelineRunner
│   ├── Samples/
│   │   ├── 1.Bootstrap/           # GameBootstrapModule、GameWorldBlueprint
│   │   ├── 2.Pipeline/            # SkillPipeline、SkillContext、Phases/
│   │   └── 3.Action/             # DamageAction、BuffAction
│   └── Tests/                     # PipelineTests、ActionTests、BootstrapTests
│
├── Samples/                        # 按功能分类的示例
│   ├── Foundation/                 # 基础模块示例
│   │   ├── HelloWorld.cs          # 最小化示例
│   │   ├── MarkerRegistry.cs      # MarkerAttribute 类型注册
│   │   ├── EventSystem.cs        # 事件总线
│   │   └── ObjectPool.cs         # 对象池
│   ├── Flow/                      # Flow 流程引擎示例
│   │   ├── FlowBasics.cs         # 基础用法
│   │   ├── SequenceAndRace.cs    # Sequence / Race 组合
│   │   └── TimedFlow.cs          # 带时间的流程
│   ├── StateMachine/              # HFSM 状态机示例
│   │   ├── HFSMWithTriggers.cs  # 事件触发转换
│   │   ├── HFSMWithActions.cs   # IAction 行为层
│   │   ├── HFSMWithHierarchy.cs # 层级状态机
│   │   ├── HFSMWithBehaviors.cs # 行为树集成
│   │   └── HFSMBehaviorTree.cs  # 行为树扩展
│   ├── Triggering/                # 触发器系统示例
│   │   ├── BasicTrigger.cs       # 基础触发器
│   │   ├── TriggerWithCondition.cs # 条件触发器
│   │   ├── TriggerWithBlackboard.cs # Blackboard 数据共享
│   │   └── TriggerPlanExample.cs # 配置化触发器
│   ├── Continuous/                # 持续行为系统示例
│   │   └── ContinuousBasics.cs
│   ├── World/                     # World / DI 示例
│   │   ├── WorldLifecycle.cs      # World 创建与销毁
│   │   ├── WorldDIOverview.cs    # 依赖注入
│   │   ├── WorldServicesDeepDive.cs # 服务深度解析
│   │   ├── WorldBlueprintUsage.cs # Blueprint 用法
│   │   ├── WorldHostOverview.cs  # Host 概览
│   │   └── HostClientManagement.cs # 客户端管理
│   ├── Modifiers/                 # 属性修改器示例
│   │   ├── ModifierBasics.cs
│   │   ├── HFSMWithModifierIntegration.cs
│   │   └── DataDrivenHFSMWithOngoingBehaviors.cs
│   ├── Targeting/                 # 目标查找示例
│   │   └── TargetingBasics.cs
│   └── Demo/                      # 综合演示
│       ├── TowerDefense.cs
│       ├── TimedTowerDefense.cs
│       └── ProgressiveSkill/       # 渐进式技能示例（多个 Phase）
│
└── Infrastructure/                # 示例基础设施
    └── Config/
        └── ConfigModels.cs       # 配置数据模型
```

## 示例分类

| 分类 | 示例文件 | 主题 |
|------|---------|------|
| **Foundation** | `HelloWorld.cs` | 最小化框架使用 |
| **Foundation** | `MarkerRegistry.cs` | 基于 Attribute 的类型注册 |
| **Foundation** | `EventSystem.cs` | 事件总线 |
| **Pipeline** | `PhaseExecutorRegistry.cs` | Phase 执行器注册 |
| **Flow** | `FlowBasics.cs` | Flow 基础用法 |
| **Flow** | `SequenceAndRace.cs` | 组合节点 |
| **StateMachine** | `HFSMWithTriggers.cs` | 事件驱动转换 |
| **StateMachine** | `HFSMWithActions.cs` | IAction 行为层 |
| **Triggering** | `BasicTrigger.cs` | 基础触发器 |
| **Triggering** | `TriggerPlanExample.cs` | 数据驱动的 TriggerPlan |
| **World** | `WorldLifecycle.cs` | World 生命周期 |
| **World** | `WorldDIOverview.cs` | DI 容器用法 |

## 运行示例

所有示例均为纯 C# 代码，可直接在 Visual Studio 或 Rider 中运行：

```powershell
# 在 src/ 目录下编译所有项目
cd src
dotnet build

# 或直接编译特定项目
dotnet build AbilityKit.Samples.Logic/AbilityKit.Samples.Logic.csproj
```

示例代码大多为独立片段，展示了各模块的核心 API 用法。可直接参考 `Samples/` 目录下对应模块的 `.cs` 文件。
