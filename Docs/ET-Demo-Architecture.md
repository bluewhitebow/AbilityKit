# ET Demo 架构设计文档

## 一、设计目标

### 1.1 核心原则

1. **复用 AbilityKit 核心** - 使用 Trigger/Pipeline/Effect 系统作为战斗逻辑引擎
2. **复用 Moba.Console 架构** - Feature 模块化 + HFSM 状态机 + ECS 实体
3. **ET 风格实现** - Component + System + Event 模式
4. **跨平台** - 不依赖 Unity API，可在 Console 环境运行

### 1.2 架构分层

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           ET Demo 架构分层                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐ │
│  │                     Platform Layer (平台层)                             │ │
│  │                                                                       │ │
│  │  Unity.View.Runtime          Console.View.Runtime                      │ │
│  │  实现：GameObject 渲染       实现：ASCII 渲染                          │ │
│  └───────────────────────────────────────────────────────────────────────┘ │
│                                    │                                        │
│  ┌─────────────────────────────────┴─────────────────────────────────────┐ │
│  │                    Share Layer (共享层)                                │ │
│  │                                                                       │ │
│  │  IBattleViewEventSink, IBattleInputSink, BattleContextData            │ │
│  │  IBattleFlowHandler, ITriggerContext                                  │ │
│  │  定义接口契约 + AbilityKit 核心类型                                    │ │
│  └───────────────────────────────────────────────────────────────────────┘ │
│                                    │                                        │
│  ┌─────────────────────────────────┴─────────────────────────────────────┐ │
│  │                     Core Layer (核心层)                                │ │
│  │                                                                       │ │
│  │  AbilityKit.Ability        - 技能/效果系统                            │ │
│  │  AbilityKit.Triggering     - 触发器系统                               │ │
│  │  AbilityKit.Pipeline      - 技能管线                                 │ │
│  │  AbilityKit.Continuous    - 持续体系统                               │ │
│  │  AbilityKit.GameplayTags  - 标签系统                                 │ │
│  │  AbilityKit.Attributes    - 属性系统                                 │ │
│  └───────────────────────────────────────────────────────────────────────┘ │
│                                    │                                        │
│  ┌─────────────────────────────────┴─────────────────────────────────────┐ │
│  │                     Game Layer (游戏层)                                 │ │
│  │                                                                       │ │
│  │  ET.AbilityKit.Demo.ET.Logic   - 逻辑层 (Component + System)         │ │
│  │  ET.AbilityKit.Demo.ET.View    - 视图层 (ET 风格实现)                 │ │
│  └───────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 二、目录结构

```
src/AbilityKit.Demo.ET/
│
├── AbilityKit.Demo.ET.Share/                    # 共享层
│   ├── Model/
│   │   ├── Share/
│   │   │   ├── ETDemoSceneType.cs              # 场景类型枚举
│   │   │   ├── BattleStartPlan.cs              # 战斗启动计划
│   │   │   ├── BattleState.cs                 # 战斗状态枚举
│   │   │   └── ActorData.cs                   # Actor 数据
│   │   ├── Events/
│   │   │   ├── ActorSpawnEvent.cs              # 单位生成事件
│   │   │   ├── ActorDeadEvent.cs              # 单位死亡事件
│   │   │   ├── ActorMoveEvent.cs              # 单位移动事件
│   │   │   ├── ActorDamageEvent.cs            # 单位受伤事件
│   │   │   ├── BattleStartEvent.cs            # 战斗开始事件
│   │   │   ├── BattleEndEvent.cs              # 战斗结束事件
│   │   │   └── FrameTickEvent.cs              # 帧Tick事件
│   │   └── EventType.cs                       # 事件类型定义
│   └── Interface/
│       ├── IBattleViewEventSink.cs            # 视图事件Sink接口
│       ├── IBattleInputSink.cs                # 输入Sink接口
│       ├── IBattleContextSink.cs              # 战斗上下文Sink接口
│       └── IBattleFlowHandler.cs              # 战斗流程处理器接口
│
├── AbilityKit.Demo.ET.Logic/                    # 逻辑层
│   ├── Model/
│   │   ├── Battle/
│   │   │   ├── ETBattleComponent.cs           # 战斗管理器Component
│   │   │   ├── ETUnitComponent.cs            # 单位管理器Component
│   │   │   ├── ETUnit.cs                     # 单位Entity
│   │   │   ├── ETSessionComponent.cs          # 会话Component
│   │   │   ├── ETFlowComponent.cs             # 流程Component
│   │   │   ├── ETInputComponent.cs            # 输入Component
│   │   │   └── ETTriggerComponent.cs          # 触发器Component
│   │   ├── Input/
│   │   │   ├── MoveCommand.cs                 # 移动命令
│   │   │   ├── SkillCommand.cs                # 技能命令
│   │   │   └── InputBuffer.cs                 # 输入缓冲
│   │   └── Flow/
│   │       ├── FlowPhase.cs                   # 流程阶段枚举
│   │       └── FlowStep.cs                   # 流程步骤枚举
│   └── Hotfix/Share/
│       ├── Battle/
│       │   ├── ETBattleComponentSystem.cs     # 战斗管理器System
│       │   ├── ETUnitComponentSystem.cs       # 单位管理器System
│       │   ├── ETUnitSystem.cs                # 单位System
│       │   ├── ETSessionComponentSystem.cs    # 会话System
│       │   └── ETFlowComponentSystem.cs       # 流程System
│       ├── Input/
│       │   ├── ETInputComponentSystem.cs      # 输入System
│       │   └── ETInputBufferSystem.cs        # 输入缓冲System
│       ├── Trigger/
│       │   ├── ETTriggerComponentSystem.cs    # 触发器System
│       │   └── AbilityKit Trigger 配置
│       └── Events/
│           ├── BattleEventHandlers.cs          # 战斗事件处理
│           ├── ActorEventHandlers.cs           # 单位事件处理
│           └── FlowEventHandlers.cs            # 流程事件处理
│
├── AbilityKit.Demo.ET.View/                     # 视图层
│   ├── ModelView/Client/
│   │   ├── Battle/
│   │   │   ├── ETBattleViewComponent.cs       # 视图管理器Component
│   │   │   └── ETUnitViewComponent.cs        # 单位视图Component
│   │   └── Unit/
│   │       └── ETUnitViewComponent.cs
│   └── ModelView/Client/Battle/
│       └── ETBattleViewComponentSystem.cs     # 视图管理器System
│
└── AbilityKit.Demo.ET.App/                     # 启动入口
    ├── Entry/
    │   ├── ETDemoEntry.cs                    # 入口类
    │   └── ETDemoApp.cs                      # App类
    └── Program.cs
```

---

## 三、核心接口设计

### 3.1 IBattleViewEventSink

**文件**: `Share/Interface/IBattleViewEventSink.cs`

```csharp
namespace ET.AbilityKit.Demo.ET.Share
{
    /// <summary>
    /// 视图事件 Sink - 逻辑层通过此接口通知视图层
    /// </summary>
    public interface IBattleViewEventSink
    {
        // 单位事件
        void OnActorSpawn(ActorSpawnEvent evt);
        void OnActorDead(ActorDeadEvent evt);
        void OnActorMove(ActorMoveEvent evt);
        void OnActorDamage(ActorDamageEvent evt);
        void OnActorHealthChange(long actorId, float currentHp, float maxHp);

        // 技能事件
        void OnSkillCast(long casterId, int skillId, float targetX, float targetY);
        void OnSkillHit(long targetId, int skillId, float damage);

        // 特效事件
        void OnVfxSpawn(long actorId, string vfxId, float x, float y);
        void OnFloatingText(long actorId, string text, float x, float y, string textType);

        // 战斗事件
        void OnBattleStart(BattleStartEvent evt);
        void OnBattleEnd(BattleEndEvent evt);
    }
}
```

### 3.2 IBattleInputSink

**文件**: `Share/Interface/IBattleInputSink.cs`

```csharp
namespace ET.AbilityKit.Demo.ET.Share
{
    /// <summary>
    /// 输入 Sink - 输入层通过此接口提交输入到逻辑层
    /// </summary>
    public interface IBattleInputSink
    {
        /// <summary>
        /// 提交移动输入
        /// </summary>
        void SubmitMoveInput(int frame, long actorId, float x, float y);

        /// <summary>
        /// 提交技能输入
        /// </summary>
        void SubmitSkillInput(int frame, long actorId, int skillSlot, float targetX, float targetY);

        /// <summary>
        /// 提交停止输入
        /// </summary>
        void SubmitStopInput(int frame, long actorId);
    }
}
```

### 3.3 IBattleContextSink

**文件**: `Share/Interface/IBattleContextSink.cs`

```csharp
namespace ET.AbilityKit.Demo.ET.Share
{
    /// <summary>
    /// 战斗上下文 Sink - 提供战斗上下文数据访问
    /// </summary>
    public interface IBattleContextSink
    {
        /// <summary>
        /// 获取当前帧号
        /// </summary>
        int CurrentFrame { get; }

        /// <summary>
        /// 获取逻辑时间
        /// </summary>
        float LogicTimeSeconds { get; }

        /// <summary>
        /// 获取本地玩家ID
        /// </summary>
        long LocalActorId { get; }

        /// <summary>
        /// 获取战斗状态
        /// </summary>
        BattleState State { get; }

        /// <summary>
        /// 获取单位数据
        /// </summary>
        ActorData? GetActor(long actorId);
    }
}
```

---

## 四、Component 设计

### 4.1 ETBattleComponent

**文件**: `Logic/Model/Battle/ETBattleComponent.cs`

```csharp
/// <summary>
/// 战斗管理器组件 - 只定义数据
/// </summary>
[ComponentOf(typeof(Scene))]
public class ETBattleComponent: Entity, IAwake, IUpdate
{
    // 战斗标识
    public long BattleId { get; set; }
    public long PlayerId { get; set; }
    public long PlayerActorId { get; set; }

    // 战斗状态
    public BattleState State { get; set; } = BattleState.Idle;
    public FlowPhase CurrentFlowPhase { get; set; } = FlowPhase.None;

    // 帧同步数据
    public int CurrentFrame { get; set; }
    public float LogicTimeSeconds { get; set; }
    public int TargetFrame { get; set; }

    // 依赖注入
    public IBattleViewEventSink ViewSink { get; set; }
    public IBattleInputSink InputSink { get; set; }
    public IBattleContextSink ContextSink { get; set; }

    // 单位管理
    public long ETUnitComponentId { get; set; }
    public long ETInputComponentId { get; set; }
    public long ETFlowComponentId { get; set; }
}
```

### 4.2 ETUnitComponent

**文件**: `Logic/Model/Battle/ETUnitComponent.cs`

```csharp
/// <summary>
/// 单位管理器组件 - 只定义数据
/// </summary>
[ComponentOf(typeof(Scene))]
public class ETUnitComponent: Entity, IAwake
{
    // 单位字典 (由 System 管理)
    // 使用 Dictionary<long, ETUnit> 存储单位
}
```

### 4.3 ETUnit

**文件**: `Logic/Model/Battle/ETUnit.cs`

```csharp
/// <summary>
/// 单位实体 - 只定义数据
/// </summary>
[ComponentOf(typeof(Scene))]
public class ETUnit: Entity, IAwake
{
    // 标识
    public long ActorId { get; set; }
    public int EntityCode { get; set; }
    public ActorKind Kind { get; set; }

    // 基础属性
    public string Name { get; set; }

    // 变换
    public float X { get; set; }
    public float Y { get; set; }
    public float Rotation { get; set; }

    // 战斗属性
    public float Hp { get; set; }
    public float MaxHp { get; set; }
    public float Attack { get; set; }
    public float Defense { get; set; }
    public float MoveSpeed { get; set; }

    // 状态
    public bool IsDead => Hp <= 0;
    public bool IsLocalPlayer { get; set; }

    // AbilityKit 集成
    public long AbilityEntityId { get; set; }  // 对应 AbilityKit 的 UnitFacade
}
```

### 4.4 ETSessionComponent

**文件**: `Logic/Model/Battle/ETSessionComponent.cs`

```csharp
/// <summary>
/// 会话组件 - 管理战斗会话生命周期
/// </summary>
[ComponentOf(typeof(Scene))]
public class ETSessionComponent: Entity, IAwake
{
    // 会话状态
    public bool IsActive { get; set; }
    public long StartTimeSeconds { get; set; }
    public int LastProcessedFrame { get; set; }
    public float TickAccumulator { get; set; }

    // 帧率配置
    public float FrameRate { get; set; } = 30f;
    public float FrameInterval => 1f / FrameRate;

    // 钩子引用
    public long ETFlowComponentId { get; set; }
}
```

### 4.5 ETInputComponent

**文件**: `Logic/Model/Input/ETInputComponent.cs`

```csharp
/// <summary>
/// 输入组件 - 管理输入缓冲
/// </summary>
[ComponentOf(typeof(Scene))]
public class ETInputComponent: Entity, IAwake
{
    // 输入缓冲 (由 System 管理)
    // 使用 List<InputCommand> 存储待处理的输入

    // 当前位置
    public float MoveTargetX { get; set; }
    public float MoveTargetY { get; set; }
    public int CurrentSkillSlot { get; set; } = -1;
    public float SkillTargetX { get; set; }
    public float SkillTargetY { get; set; }
}
```

### 4.6 ETFlowComponent

**文件**: `Logic/Model/Flow/ETFlowComponent.cs`

```csharp
/// <summary>
/// 流程组件 - 管理战斗流程状态
/// </summary>
[ComponentOf(typeof(Scene))]
public class ETFlowComponent: Entity, IAwake, IUpdate
{
    // 流程状态
    public FlowPhase CurrentPhase { get; set; } = FlowPhase.None;
    public FlowStep CurrentStep { get; set; } = FlowStep.None;

    // 流程数据
    public int StepsCompleted { get; set; }
    public bool IsTransitioning { get; set; }

    // Feature 引用
    public long BattleFeatureHostId { get; set; }
    public long ViewFeatureHostId { get; set; }
}
```

---

## 五、System 设计

### 5.1 ETBattleComponentSystem

**文件**: `Logic/Hotfix/Share/Battle/ETBattleComponentSystem.cs`

```csharp
[EntitySystemOf(typeof(ETBattleComponent))]
[FriendOf(typeof(ETBattleComponent))]
[FriendOf(typeof(ETUnitComponent))]
[FriendOf(typeof(ETUnit))]
public static partial class ETBattleComponentSystem
{
    [EntitySystem]
    private static void Awake(this ETBattleComponent self)
    {
        Log.Info("[ETBattle] ETBattleComponent awake");
    }

    [EntitySystem]
    private static void Update(this ETBattleComponent self)
    {
        // 获取 Session 执行帧推进
        var session = self.Scene().GetComponent<ETSessionComponent>();
        if (session == null || !session.IsActive)
            return;

        // 帧同步循环
        session.TickAccumulator += TimeHelper.DeltaTime;
        while (session.TickAccumulator >= session.FrameInterval)
        {
            session.TickAccumulator -= session.FrameInterval;
            self.AdvanceFrame();
        }
    }

    /// <summary>
    /// 初始化战斗
    /// </summary>
    public static void InitializeBattle(this ETBattleComponent self, BattleStartPlan plan)
    {
        self.BattleId = IdGenerater.Instance.GenerateId();
        self.PlayerId = plan.PlayerId;
        self.PlayerActorId = plan.LocalActorId;
        self.State = BattleState.Loading;

        Log.Info($"[ETBattle] Initializing battle {self.BattleId}...");

        // 创建依赖组件
        var unitComponent = self.Scene().AddComponent<ETUnitComponent>();
        self.ETUnitComponentId = unitComponent.InstanceId;

        var inputComponent = self.Scene().AddComponent<ETInputComponent>();
        self.ETInputComponentId = inputComponent.InstanceId;

        var flowComponent = self.Scene().AddComponent<ETFlowComponent>();
        self.ETFlowComponentId = flowComponent.InstanceId;

        var session = self.Scene().AddComponent<ETSessionComponent>();
        session.ETFlowComponentId = flowComponent.InstanceId;

        self.State = BattleState.Ready;
        Log.Info($"[ETBattle] Battle {self.BattleId} ready!");
    }

    /// <summary>
    /// 前进一帧
    /// </summary>
    public static void AdvanceFrame(this ETBattleComponent self)
    {
        self.CurrentFrame++;
        self.LogicTimeSeconds = (float)self.CurrentFrame / 30f;

        // 1. 处理输入
        var inputComponent = self.Scene().GetComponent<ETInputComponent>();
        inputComponent?.ProcessInput(self.CurrentFrame);

        // 2. 执行战斗逻辑
        var flowComponent = self.Scene().GetComponent<ETFlowComponent>();
        flowComponent?.Tick();

        // 3. 更新单位状态
        var unitComponent = self.Scene().GetComponent<ETUnitComponent>();
        unitComponent?.Tick(self.LogicTimeSeconds);

        // 4. 发布帧Tick事件
        EventSystem.Instance.Publish<Scene, FrameTickEvent>(self.Scene(), new FrameTickEvent()
        {
            Frame = self.CurrentFrame,
            TimeSeconds = self.LogicTimeSeconds
        });
    }

    /// <summary>
    /// 开始战斗
    /// </summary>
    public static void StartBattle(this ETBattleComponent self)
    {
        if (self.State != BattleState.Ready)
        {
            Log.Warning($"[ETBattle] Cannot start battle, current state: {self.State}");
            return;
        }

        self.State = BattleState.InProgress;

        var session = self.Scene().GetComponent<ETSessionComponent>();
        session.IsActive = true;

        Log.Info($"[ETBattle] Battle {self.BattleId} started!");

        // 发布战斗开始事件
        self.ViewSink?.OnBattleStart(new BattleStartEvent()
        {
            BattleId = self.BattleId,
            PlayerId = self.PlayerId
        });
    }

    /// <summary>
    /// 结束战斗
    /// </summary>
    public static void EndBattle(this ETBattleComponent self, bool isVictory)
    {
        if (self.State != BattleState.InProgress)
            return;

        self.State = BattleState.Ended;

        var session = self.Scene().GetComponent<ETSessionComponent>();
        session.IsActive = false;

        Log.Info($"[ETBattle] Battle {self.BattleId} ended: {(isVictory ? "VICTORY" : "DEFEAT")}");

        // 发布战斗结束事件
        self.ViewSink?.OnBattleEnd(new BattleEndEvent()
        {
            BattleId = self.BattleId,
            IsVictory = isVictory
        });
    }
}
```

### 5.2 ETUnitComponentSystem

**文件**: `Logic/Hotfix/Share/Battle/ETUnitComponentSystem.cs`

```csharp
[EntitySystemOf(typeof(ETUnitComponent))]
[FriendOf(typeof(ETUnitComponent))]
[FriendOf(typeof(ETUnit))]
public static partial class ETUnitComponentSystem
{
    // 单位字典 (静态存储，或使用 Component 字段)
    private static readonly Dictionary<long, ETUnit> _units = new();

    [EntitySystem]
    private static void Awake(this ETUnitComponent self)
    {
        _units.Clear();
    }

    [EntitySystem]
    private static void Destroy(this ETUnitComponent self)
    {
        foreach (var unit in _units.Values)
        {
            unit.Dispose();
        }
        _units.Clear();
    }

    /// <summary>
    /// 创建单位
    /// </summary>
    public static ETUnit CreateUnit(this ETUnitComponent self, long actorId, int entityCode, ActorKind kind, string name)
    {
        var unit = self.AddChild<ETUnit>();
        unit.ActorId = actorId;
        unit.EntityCode = entityCode;
        unit.Kind = kind;
        unit.Name = name;
        unit.MaxHp = 100f;
        unit.Hp = 100f;
        unit.Attack = 10f;
        unit.Defense = 5f;
        unit.MoveSpeed = 5f;
        unit.X = 0;
        unit.Y = 0;

        _units[actorId] = unit;

        Log.Info($"[ETUnit] Unit created: {name} ({actorId})");

        return unit;
    }

    /// <summary>
    /// 获取单位
    /// </summary>
    public static ETUnit? GetUnit(this ETUnitComponent self, long actorId)
    {
        return _units.TryGetValue(actorId, out var unit) ? unit : null;
    }

    /// <summary>
    /// Tick - 每帧更新所有单位
    /// </summary>
    public static void Tick(this ETUnitComponent self, float deltaTime)
    {
        foreach (var unit in _units.Values)
        {
            if (!unit.IsDead)
            {
                // 更新单位状态
                unit.TickUnit(deltaTime);
            }
        }
    }

    /// <summary>
    /// 应用伤害
    /// </summary>
    public static void ApplyDamage(this ETUnitComponent self, long targetActorId, float damage)
    {
        var unit = _units.GetValueOrDefault(targetActorId);
        if (unit == null || unit.IsDead)
            return;

        float actualDamage = Math.Max(1f, damage - unit.Defense * 0.5f);
        unit.Hp = Math.Max(0, unit.Hp - actualDamage);

        Log.Info($"[ETUnit] {unit.Name} took {actualDamage:F1} damage, HP: {unit.Hp:F0}/{unit.MaxHp}");

        // 通知视图层
        var battleComponent = self.Scene().GetComponent<ETBattleComponent>();
        battleComponent.ViewSink?.OnActorDamage(new ActorDamageEvent()
        {
            ActorId = targetActorId,
            Damage = actualDamage,
            CurrentHp = unit.Hp,
            MaxHp = unit.MaxHp
        });

        if (unit.IsDead)
        {
            unit.OnDead();
        }
    }
}
```

### 5.3 ETUnitSystem

**文件**: `Logic/Hotfix/Share/Battle/ETUnitSystem.cs`

```csharp
[EntitySystemOf(typeof(ETUnit))]
[FriendOf(typeof(ETUnit))]
public static partial class ETUnitSystem
{
    [EntitySystem]
    private static void Awake(this ETUnit self)
    {
    }

    /// <summary>
    /// Tick 单位
    /// </summary>
    public static void TickUnit(this ETUnit self, float deltaTime)
    {
        // 更新移动 (如果正在移动)
        // 更新Buff/Effect
    }

    /// <summary>
    /// 移动到目标位置
    /// </summary>
    public static void MoveTo(this ETUnit self, float targetX, float targetY)
    {
        float dx = targetX - self.X;
        float dy = targetY - self.Y;
        float distance = (float)Math.Sqrt(dx * dx + dy * dy);

        if (distance < 0.1f)
            return;

        // 简单线性移动
        float moveDistance = self.MoveSpeed * TimeHelper.DeltaTime;
        if (moveDistance > distance)
            moveDistance = distance;

        float ratio = moveDistance / distance;
        self.X += dx * ratio;
        self.Y += dy * ratio;

        // 更新旋转
        self.Rotation = (float)Math.Atan2(dy, dx);
    }

    /// <summary>
    /// 单位死亡
    /// </summary>
    private static void OnDead(this ETUnit self)
    {
        Log.Info($"[ETUnit] {self.Name} is dead!");

        // 发布死亡事件
        EventSystem.Instance.Publish<Scene, ActorDeadEvent>(self.Scene(), new ActorDeadEvent()
        {
            ActorId = self.ActorId,
            KillerId = 0  // TODO: 记录击杀者
        });
    }
}
```

---

## 六、视图层设计

### 6.1 ETBattleViewComponent

**文件**: `View/ModelView/Client/Battle/ETBattleViewComponent.cs`

```csharp
/// <summary>
/// 战斗视图组件 - 只定义数据
/// </summary>
[ComponentOf(typeof(Scene))]
public class ETBattleViewComponent: Entity, IAwake
{
    // 视图配置
    public int ViewWidth { get; set; } = 80;
    public int ViewHeight { get; set; } = 30;
    public float UnitDisplayScale { get; set; } = 1f;

    // 视图组件引用
    public long ETUnitViewComponentId { get; set; }

    // 事件订阅
    public long BattleComponentId { get; set; }
}
```

### 6.2 ETBattleViewComponentSystem

**文件**: `View/ModelView/Client/Battle/ETBattleViewComponentSystem.cs`

```csharp
[EntitySystemOf(typeof(ETBattleViewComponent))]
public static partial class ETBattleViewComponentSystem
{
    [EntitySystem]
    private static void Awake(this ETBattleViewComponent self)
    {
        Log.Info("[ETBattleView] ETBattleViewComponent awake");

        // 订阅事件
        self.Subscribe<ActorSpawnEvent>(self.OnActorSpawn);
        self.Subscribe<ActorDeadEvent>(self.OnActorDead);
        self.Subscribe<ActorMoveEvent>(self.OnActorMove);
        self.Subscribe<ActorDamageEvent>(self.OnActorDamage);
        self.Subscribe<BattleStartEvent>(self.OnBattleStart);
        self.Subscribe<BattleEndEvent>(self.OnBattleEnd);
    }

    // 事件处理
    private static void OnActorSpawn(this ETBattleViewComponent self, ActorSpawnEvent evt)
    {
        var unitViewComponent = self.Scene().GetComponent<ETUnitViewComponent>();
        unitViewComponent.CreateUnitView(evt.ActorId, evt.Name, evt.X, evt.Y);

        Console.WriteLine($"[ETBattleView] Unit spawned: {evt.Name} at ({evt.X:F1}, {evt.Y:F1})");
    }

    private static void OnActorDead(this ETBattleViewComponent self, ActorDeadEvent evt)
    {
        var unitViewComponent = self.Scene().GetComponent<ETUnitViewComponent>();
        unitViewComponent.DestroyUnitView(evt.ActorId);

        Console.WriteLine($"[ETBattleView] Unit dead: {evt.ActorId}");
    }

    private static void OnActorMove(this ETBattleViewComponent self, ActorMoveEvent evt)
    {
        var unitViewComponent = self.Scene().GetComponent<ETUnitViewComponent>();
        unitViewComponent.UpdateUnitPosition(evt.ActorId, evt.X, evt.Y);
    }

    private static void OnActorDamage(this ETBattleViewComponent self, ActorDamageEvent evt)
    {
        var unitViewComponent = self.Scene().GetComponent<ETUnitViewComponent>();
        unitViewComponent.ShowFloatingText(evt.ActorId, $"-{evt.Damage:F0}", "damage");

        Console.WriteLine($"[ETBattleView] Damage: {evt.ActorId} took {evt.Damage:F0} damage");
    }

    private static void OnBattleStart(this ETBattleViewComponent self, BattleStartEvent evt)
    {
        Console.WriteLine("========================================");
        Console.WriteLine($"[ETBattleView] Battle {evt.BattleId} STARTED!");
        Console.WriteLine("========================================");
    }

    private static void OnBattleEnd(this ETBattleViewComponent self, BattleEndEvent evt)
    {
        Console.WriteLine("========================================");
        Console.WriteLine($"[ETBattleView] Battle {evt.BattleId} ENDED: {(evt.IsVictory ? "VICTORY" : "DEFEAT")}");
        Console.WriteLine("========================================");
    }

    /// <summary>
    /// 渲染视图
    /// </summary>
    public static void Render(this ETBattleViewComponent self)
    {
        // 清屏
        Console.Clear();

        // 获取单位组件
        var unitComponent = self.Scene().GetComponent<ETUnitComponent>();
        if (unitComponent == null)
            return;

        // 渲染所有单位
        foreach (var unit in unitComponent.GetAllUnits())
        {
            int x = (int)(unit.X + self.ViewWidth / 2);
            int y = (int)(self.ViewHeight - unit.Y);

            if (x >= 0 && x < self.ViewWidth && y >= 0 && y < self.ViewHeight)
            {
                string symbol = unit.Kind == ActorKind.Character ? "@" : "M";
                string color = unit.IsDead ? "\x1b[31m" : "\x1b[32m";  // 红色死亡，绿色存活
                Console.WriteLine($"{color}{symbol} {unit.Name} HP:{unit.Hp:F0}/{unit.MaxHp}\x1b[0m");
            }
        }
    }
}
```

---

## 七、事件定义

### 7.1 事件结构体

**文件**: `Share/Model/Events/*.cs`

```csharp
namespace ET.AbilityKit.Demo.ET.Share
{
    // 单位事件
    public struct ActorSpawnEvent: IEvent
    {
        public Type Type => typeof(ActorSpawnEvent);
        public long ActorId;
        public int EntityCode;
        public ActorKind Kind;
        public string Name;
        public float X;
        public float Y;
        public float MaxHp;
    }

    public struct ActorDeadEvent: IEvent
    {
        public Type Type => typeof(ActorDeadEvent);
        public long ActorId;
        public long KillerId;
    }

    public struct ActorMoveEvent: IEvent
    {
        public Type Type => typeof(ActorMoveEvent);
        public long ActorId;
        public float X;
        public float Y;
    }

    public struct ActorDamageEvent: IEvent
    {
        public Type Type => typeof(ActorDamageEvent);
        public long ActorId;
        public long SourceActorId;
        public float Damage;
        public float CurrentHp;
        public float MaxHp;
    }

    // 战斗事件
    public struct BattleStartEvent: IEvent
    {
        public Type Type => typeof(BattleStartEvent);
        public long BattleId;
        public long PlayerId;
    }

    public struct BattleEndEvent: IEvent
    {
        public Type Type => typeof(BattleEndEvent);
        public long BattleId;
        public bool IsVictory;
    }

    public struct FrameTickEvent: IEvent
    {
        public Type Type => typeof(FrameTickEvent);
        public int Frame;
        public float TimeSeconds;
    }
}
```

---

## 八、流程状态机

### 8.1 流程阶段枚举

```csharp
public enum FlowPhase
{
    None,
    Prepare,       // 准备阶段
    Connect,       // 连接阶段
    CreateWorld,   // 创建世界
    LoadAssets,    // 加载资源
    InMatch,       // 战斗中
    End            // 结束
}

public enum FlowStep
{
    None,

    // Prepare 阶段步骤
    Prepare_Initialize,

    // Connect 阶段步骤
    Connect_Connect,
    Connect_WaitPlayers,

    // CreateWorld 阶段步骤
    CreateWorld_CreateEntities,
    CreateWorld_RegisterPlayers,

    // LoadAssets 阶段步骤
    LoadAssets_LoadResources,
    LoadAssets_NotifyReady,

    // InMatch 阶段步骤
    InMatch_StartBattle,
    InMatch_BattleLoop,
    InMatch_CheckEnd,

    // End 阶段步骤
    End_Cleanup,
    End_Finished
}
```

### 8.2 流程组件System

```csharp
[EntitySystemOf(typeof(ETFlowComponent))]
public static partial class ETFlowComponentSystem
{
    [EntitySystem]
    private static void Awake(this ETFlowComponent self)
    {
        Log.Info("[ETFlow] ETFlowComponent awake");
    }

    [EntitySystem]
    private static void Update(this ETFlowComponent self)
    {
        switch (self.CurrentPhase)
        {
            case FlowPhase.Prepare:
                self.TickPrepare();
                break;
            case FlowPhase.Connect:
                self.TickConnect();
                break;
            case FlowPhase.CreateWorld:
                self.TickCreateWorld();
                break;
            case FlowPhase.LoadAssets:
                self.TickLoadAssets();
                break;
            case FlowPhase.InMatch:
                self.TickInMatch();
                break;
        }
    }

    private static void TickInMatch(this ETFlowComponent self)
    {
        switch (self.CurrentStep)
        {
            case FlowStep.None:
                self.TransitionTo(FlowPhase.InMatch, FlowStep.InMatch_StartBattle);
                break;

            case FlowStep.InMatch_StartBattle:
                self.DoStartBattle();
                self.TransitionTo(FlowPhase.InMatch, FlowStep.InMatch_BattleLoop);
                break;

            case FlowStep.InMatch_BattleLoop:
                // 战斗循环由 BattleComponent.Update 驱动
                break;

            case FlowStep.InMatch_CheckEnd:
                self.CheckBattleEnd();
                break;
        }
    }

    private static void TransitionTo(this ETFlowComponent self, FlowPhase phase, FlowStep step)
    {
        Log.Info($"[ETFlow] Transition: {self.CurrentPhase}.{self.CurrentStep} -> {phase}.{step}");
        self.CurrentPhase = phase;
        self.CurrentStep = step;
        self.IsTransitioning = false;
    }

    private static void DoStartBattle(this ETFlowComponent self)
    {
        var battleComponent = self.Scene().GetComponent<ETBattleComponent>();
        battleComponent.StartBattle();
    }
}
```

---

## 九、依赖关系图

```
┌────────────────────────────────────────────────────────────────────────────────┐
│                           组件依赖关系图                                        │
├────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  Scene                                                                          │
│   ├── ETBattleComponent ─────────────────┐                                     │
│   │      │                                │                                     │
│   │      ├──► ETUnitComponent            │                                     │
│   │      │        └──► ETUnit[]           │                                     │
│   │      │                                │                                     │
│   │      ├──► ETInputComponent           │                                     │
│   │      │                                │                                     │
│   │      ├──► ETFlowComponent            │                                     │
│   │      │                                │                                     │
│   │      └──► ETSessionComponent         │                                     │
│   │                                       │                                     │
│   │                                       │                                     │
│   └── ETBattleViewComponent ◄────────────┘                                     │
│          └──► ETUnitViewComponent                                                │
│                                                                                 │
├────────────────────────────────────────────────────────────────────────────────┤
│                           System 调用链                                          │
├────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  ETBattleComponentSystem.Update()                                              │
│       │                                                                       │
│       ├──► ETSessionComponentSystem.Tick()   - 帧推进                           │
│       │                                                                       │
│       ├──► ETInputComponentSystem.ProcessInput() - 处理输入                     │
│       │                                                                       │
│       ├──► ETFlowComponentSystem.Update()   - 流程状态机                       │
│       │                                                                       │
│       ├──► ETUnitComponentSystem.Tick()     - 单位Tick                         │
│       │        └──► ETUnitSystem.TickUnit()                                  │
│       │                                                                       │
│       └──► EventSystem.Publish(FrameTickEvent)                                 │
│                                                                                 │
├────────────────────────────────────────────────────────────────────────────────┤
│                           事件流                                                │
├────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  ETBattleComponentSystem                                                       │
│       │                                                                       │
│       ├──► IBattleViewEventSink.OnActorSpawn()  ──► ETBattleViewComponent     │
│       │                                                                       │
│       ├──► IBattleViewEventSink.OnActorMove()                                 │
│       │                                                                       │
│       ├──► IBattleViewEventSink.OnActorDamage()                               │
│       │                                                                       │
│       └──► IBattleViewEventSink.OnBattleEnd()                                 │
│                                                                                 │
└────────────────────────────────────────────────────────────────────────────────┘
```

---

## 十、与 Moba.Console 的对应关系

| Moba.Console | ET Demo | 说明 |
|--------------|---------|------|
| `ConsoleBattleContext` | `ETBattleComponent + ETUnitComponent` | 战斗上下文拆分 |
| `BattleEntityFactory` | `ETUnitComponentSystem.CreateUnit()` | 实体工厂 |
| `BattleEntity` | `ETUnit` | 单位实体 |
| `BattleNetIdComponent` | `ETUnit.ActorId` | 网络ID |
| `BattleTransformComponent` | `ETUnit.X/Y/Rotation` | 变换组件 |
| `BattleCharacterComponent` | `ETUnit.*` | 角色属性 |
| `FeatureHost` | ET FlowComponent + System | Feature生命周期 |
| `ConsoleSyncFeature` | `ETSessionComponent` | 帧同步状态 |
| `ConsoleInputFeature` | `ETInputComponentSystem` | 输入处理 |
| `ConsoleViewFeature` | `ETBattleViewComponentSystem` | 视图Feature |
| `ConsoleViewBinder` | `ETUnitViewComponent` | 视图绑定+插值 |
| `ConsoleSessionOrchestrator` | `ETFlowComponentSystem` | 会话编排 |
| `IWorldInputSink` | `IBattleInputSink` | 输入Sink |
| `BaseBattleViewEventSink` | `IBattleViewEventSink` | 视图事件Sink |

---

## 十一、实施计划

### 阶段 1: 基础框架
- [ ] 定义 Share 层接口和事件
- [ ] 实现 Component (数据定义)
- [ ] 实现基础 System (生命周期)
- [ ] 验证编译

### 阶段 2: 战斗核心
- [ ] 实现 ETBattleComponentSystem (初始化、帧循环)
- [ ] 实现 ETUnitComponentSystem (单位管理)
- [ ] 实现 ETUnitSystem (单位行为)
- [ ] 实现 ETSessionComponent (帧同步状态)

### 阶段 3: 流程系统
- [ ] 实现 ETFlowComponentSystem (状态机)
- [ ] 实现流程阶段和步骤
- [ ] 实现阶段转换

### 阶段 4: 视图层
- [ ] 实现 ETBattleViewComponentSystem
- [ ] 实现视图事件订阅
- [ ] 实现 ASCII 渲染

### 阶段 5: 输入系统
- [ ] 实现 ETInputComponentSystem
- [ ] 实现输入缓冲
- [ ] 实现 IBattleInputSink

### 阶段 6: 集成测试
- [ ] 端到端测试
- [ ] 性能优化
- [ ] 清理代码

---

## 十二、关键设计决策

### 12.1 为什么拆分 ETBattleComponent?

Moba.Console 使用 `ConsoleBattleContext` 作为中心化数据结构，但 ET 风格倾向于使用多个 Component 分散管理：

- **优点**: 更符合 ET 的 Component 模式，易于单元测试
- **缺点**: 跨 Component 访问需要额外处理

### 12.2 为什么使用事件而不是直接调用?

```
直接调用: BattleComponent → ViewComponent.Method()
事件调用: BattleComponent → EventSystem.Publish() → ViewComponent.OnEvent()
```

- **优点**: 解耦，支持多订阅者，易于扩展
- **缺点**: 性能略低（但可接受）

### 12.3 为什么保留 IBattleViewEventSink?

即使在同一进程，也应该保留 Sink 接口：

- **接口隔离**: 逻辑层不直接依赖视图层
- **易于测试**: 可以 Mock Sink 进行单元测试
- **未来扩展**: 如果需要网络同步，Sink 实现可以替换为网络适配器
