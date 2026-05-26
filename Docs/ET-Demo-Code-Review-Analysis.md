# ET Demo 代码规范分析报告

## 一、架构概览

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ ET Demo 架构分层图 │
├─────────────────────────────────────────────────────────────────────────────┤
│ │
│ ┌─────────────────────────────────────────────────────────────────────┐ │
│ │ ET.AbilityKit.Demo.ET.Share │ │
│ │ - 事件定义 (ActorSpawnEvent, ActorMoveEvent 等) │ │
│ │ - 接口定义 (IETViewEventSink 等) │ │
│ └─────────────────────────────────────────────────────────────────────┘ │
│ │ │
│ ▼ │
│ ┌─────────────────────────────────────────────────────────────────────┐ │
│ │ ET.Logic │ │
│ │ - ETBattleComponent + System │ │
│ │ - ETUnitComponent + System │ │
│ │ - ETBattleViewEventSink → 发布事件到 ET 事件系统 │ │
│ │ - ETBattleView_EventHandler → 订阅事件，创建 Logic 层单位 │ │
│ └─────────────────────────────────────────────────────────────────────┘ │
│ │ │
│ ▼ │
│ ┌─────────────────────────────────────────────────────────────────────┐ │
│ │ ET.AbilityKit.Demo.ET.View │ │
│ │ - ActorSpawnEventHandler → 订阅事件，创建 View 层单位 │ │
│ │ - ETUnitViewComponent → 纯数据，渲染用 │ │
│ │ - ETViewEventListener → 单位视图管理器 │ │
│ └─────────────────────────────────────────────────────────────────────┘ │
│ │
└─────────────────────────────────────────────────────────────────────────────┘
```

## 二、当前问题分析

### 2.1 命名空间问题

| 问题 | 文件 | 描述 |
|-----|------|-----|
| ❌ 多余的 ET 前缀 | `ET.AbilityKit.Demo.ET.View` | 应为 `ET.AbilityKit.Demo.View` |
| ❌ Share 层命名空间不一致 | `ET.AbilityKit.Demo.ET.Share` | 应为 `ET.AbilityKit.Demo.Share` |

**影响**:
- 代码阅读困难
- 导入语句冗长
- 不符合规范命名

### 2.2 事件重复处理问题

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ 事件流分析 │
├─────────────────────────────────────────────────────────────────────────────┤
│ │
│ 1. ETBattleViewEventSink.OnEnterGameSnapshot() │
│ └── 发布 ActorSpawnEvent ───────────────────────────────────────┐ │
│ │ │
│ 2. ETBattleView_EventHandler.Run(ActorSpawnEvent) │ │
│ ├── 创建 ETUnit (Logic 层) │ │
│ └── 再次发布 ActorSpawnEvent ─────────────────────────────────┐ │
│ │ │ │
│ 3. ActorSpawnEventHandler.Run(ActorSpawnEvent) │ │ │
│ └── 创建 ETUnitView (View 层) ◀──────────────────────────────┘ │
│ │ │
│ 问题：步骤 1 和步骤 2 都发布相同事件，导致潜在重复处理 │ │
│ │
└─────────────────────────────────────────────────────────────────────────────┘
```

**问题**:
- `ETBattleViewEventSink` 发布事件后，`ETBattleView_EventHandler` 再次发布相同事件
- 这会导致事件被处理两次

**建议**:
- `ETBattleView_EventHandler` 应该只处理 Logic 层逻辑，不应该再次发布事件
- 或者将 Logic 层和 View 层的事件分开

### 2.3 静态字典问题

**文件**: `ETUnitComponentSystem.cs`

```csharp
// ❌ 问题：使用静态字典存储实体
private static readonly Dictionary<long, ETUnit> _units = new();
```

**违反规范**:
- ET 框架使用 Component 存储数据，System 操作数据
- 静态字典会导致状态在多个 Scene 间混淆
- 不符合 ET 的 Entity-Component-System 模式

**建议**:
- 将 `_units` 字典存储在 `ETUnitComponent` 实例中
- 或者使用 ET 的 `Children` 字典

### 2.4 Component 编码问题

**文件**: `ETViewEventListener.cs`

```csharp
[ComponentOf(typeof(Scene))]
public class ETViewEventListener: Entity, IAwake
{
    // ❌ 问题：缺少 using System.Diagnostics
    // 但更重要的是...
}
```

**其他问题**:
- `ETUnitViewComponent` 是纯数据类，但包含 `Log.Info` 调用
- 建议将日志移到 Handler 中

### 2.5 事件类命名不一致

| 事件类型 | 命名空间 | 说明 |
|---------|---------|------|
| `ActorSpawnEvent` | `ET.AbilityKit.Demo.ET.Share` | ✅ 一致 |
| `ActorMoveEvent` | `ET.AbilityKit.Demo.ET.Share` | ✅ 一致 |
| `BattleStartEvent` | `ET.AbilityKit.Demo.ET.Share` | ✅ 一致 |
| `ActorDamageEvent` | `ET.AbilityKit.Demo.ET.Share` | ✅ 一致 |

命名本身是一致的，但与 `IBattleViewEventSink` 中的方法名对应关系不清晰。

## 三、优化建议

### 3.1 Attribute 驱动代码拆分

当前代码使用 `[Event]` 和 `[EntitySystem]` 特性来自动注册，这是好的。但可以进一步优化：

#### 3.1.1 事件处理器自动发现

```csharp
// 当前：每个 Handler 需要手动添加 [Event] 特性
[Event(SceneType.DemoBattle)]
public class ActorSpawnEventHandler : AEvent<Scene, ActorSpawnEvent>
{
}

// 优化：创建 MarkerAttribute 自动扫描
[AttributeUsage(AttributeTargets.Class)]
public sealed class EventHandlerAttribute : Attribute
{
    public Type EventType { get; }
    public EventHandlerAttribute(Type eventType)
    {
        EventType = eventType;
    }
}

// 使用
[EventHandler(typeof(ActorSpawnEvent))]
public class ActorSpawnEventHandler : AEvent<Scene, ActorSpawnEvent>
{
}

// 自动注册器
public static class EventHandlerRegistry
{
    private static readonly Dictionary<Type, List<Type>> _handlers = new();

    public static void Register()
    {
        var assemblies = AppDomain.Current.GetAssemblies();
        foreach (var type in assemblies.SelectMany(a => a.GetTypes()))
        {
            var attr = type.GetCustomAttribute<EventHandlerAttribute>();
            if (attr != null)
            {
                _handlers.GetOrAdd(attr.EventType).Add(type);
            }
        }
    }
}
```

#### 3.1.2 FriendOf 自动推断

```csharp
// 当前：需要手动声明 FriendOf
[EntitySystemOf(typeof(ETUnitComponent))]
[FriendOf(typeof(ETUnitComponent))]
[FriendOf(typeof(ETUnit))]  // 手动添加

// 优化：自动扫描属性访问
public static class FriendOfScanner
{
    public static HashSet<Type> ScanForFriendOf(Type systemType)
    {
        var friends = new HashSet<Type>();
        var fields = systemType.GetFields(BindingFlags.NonPublic | BindingFlags.Static);
        foreach (var field in fields)
        {
            if (field.FieldType.Name.EndsWith("Component"))
            {
                friends.Add(field.FieldType);
            }
        }
        return friends;
    }
}
```

#### 3.1.3 Component 自动注册

```csharp
// 当前：手动调用 AddComponent
var unitComponent = scene.AddComponent<ETUnitComponent>();

// 优化：使用依赖注入
public interface IComponentFactory
{
    T Create<T>() where T : Entity, new();
}

public class ComponentFactory : IComponentFactory
{
    public T Create<T>() where T : Entity, new()
    {
        return new T();
    }
}

// 使用
public static class ETBattleComponentSystem
{
    private static IComponentFactory _factory;

    public static void Initialize(IComponentFactory factory)
    {
        _factory = factory;
    }

    public static void InitializeBattle(ETBattleComponent self, ...)
    {
        var unitComponent = self.Scene().AddComponent<ETUnitComponent>();
        // ...
    }
}
```

### 3.2 事件流优化

#### 方案 A：单一事件发布点

```csharp
// ETBattleViewEventSink 只发布一次
public void OnEnterGameSnapshot(in FrameSnapshotData snapshot)
{
    foreach (var spawn in snapshot.ActorSpawns)
    {
        // 发布到 ET 事件系统
        EventSystem.Instance.Publish<Scene, ActorSpawnEvent>(scene, new ActorSpawnEvent { ... });
    }
}

// ETBattleView_EventHandler 只处理 Logic 层逻辑
[Event(SceneType.DemoBattle)]
public class ETBattleView_EventHandler : AEvent<Scene, ActorSpawnEvent>
{
    protected override async ETTask Run(Scene scene, ActorSpawnEvent args)
    {
        // 只创建 Logic 层单位，不发布事件
        var unitComponent = scene.GetComponent<ETUnitComponent>();
        unitComponent.CreateUnit(...);

        // 通知 View 层
        scene.GetComponent<ETViewEventListener>()?.OnUnitCreated(args);
    }
}

// ActorSpawnEventHandler 在 View 层只创建视图
[Event(SceneType.DemoBattle)]
public class ActorSpawnEventHandler : AEvent<Scene, ActorSpawnEvent>
{
    protected override async ETTask Run(Scene scene, ActorSpawnEvent args)
    {
        var listener = scene.GetComponent<ETViewEventListener>();
        listener?.CreateUnitView(args);
    }
}
```

#### 方案 B：使用接口分离

```csharp
// 定义 Logic 层专用事件
public interface ILogicEvent { }

// View 层通过查询获取数据，不依赖事件
public interface IViewSnapshotProvider
{
    UnitSnapshot GetSnapshot(int actorId);
}
```

### 3.3 目录结构优化

```
src/AbilityKit.Demo.ET/
├── Share/
│ ├── Model/
│ │   └── Events/
│ │       ├── ActorEvents.cs      # Actor 相关事件
│ │       ├── BattleEvents.cs     # Battle 事件
│ │       └── InputEvents.cs      # 输入事件
│ └── Interface/
│     ├── IViewEventSink.cs
│     └── IViewSnapshotProvider.cs
│
├── Logic/
│ ├── Model/
│ │   ├── Battle/
│ │   │   ├── ETBattleComponent.cs
│ │   │   └── ETBattleEntityCacheComponent.cs
│ │   ├── Unit/
│ │   │   ├── ETUnitComponent.cs
│ │   │   └── ETUnit.cs
│ │   └── Input/
│ │       └── ETInputComponent.cs
│ │
│ └── Hotfix/
│     ├── Battle/
│     │   ├── ETBattleComponentSystem.cs
│     │   ├── Handlers/          # Logic 层 Handler
│     │   │   └── ActorSpawnHandler.cs
│     │   └── Bridge/
│     │       ├── ETBattleViewEventSink.cs
│     │       └── ETBattleDriverBridge.cs
│     ├── Unit/
│     │   └── ETUnitComponentSystem.cs
│     └── Input/
│         └── ETInputComponentSystem.cs
│
├── View/
│ ├── Model/
│ │   ├── ETUnitViewComponent.cs
│ │   └── ETViewEventListener.cs
│ │
│ └── Hotfix/
│     ├── Battle/
│     │   └── Handlers/          # View 层 Handler
│     │       ├── ActorSpawnHandler.cs
│     │       ├── ActorMoveHandler.cs
│     │       └── ActorDamageHandler.cs
│     └── Unit/
│         └── ETUnitViewComponentSystem.cs
│
└── App/
    └── Entry/
        └── DemoEntry.cs
```

### 3.4 代码生成优化

利用 AbilityKit 的 `MarkerAttribute` 模式：

```csharp
// 定义事件特性
[AttributeUsage(AttributeTargets.Class)]
public sealed class EventHandlerIdAttribute : MarkerAttribute
{
    public int HandlerId { get; }
    public EventHandlerIdAttribute(int handlerId)
    {
        HandlerId = handlerId;
    }
}

// 使用
[EventHandlerId(1001)]
public class ActorSpawnHandler : IEventHandler
{
    public void Handle(Event evt) { }
}

// 自动注册
public static class EventHandlerRegistry
{
    private static readonly Dictionary<int, IEventHandler> _handlers = new();

    public static void ScanAndRegister(Assembly[] assemblies)
    {
        foreach (var type in assemblies.SelectMany(a => a.GetTypes()))
        {
            var attr = type.GetCustomAttribute<EventHandlerIdAttribute>();
            if (attr != null && Activator.CreateInstance(type) is IEventHandler handler)
            {
                _handlers[attr.HandlerId] = handler;
            }
        }
    }
}
```

## 四、问题优先级

| 优先级 | 问题 | 建议 |
|-------|------|------|
| 🔴 高 | 命名空间 `ET.AbilityKit.Demo.ET.View` 多余 | 重命名为 `ET.AbilityKit.Demo.View` |
| 🔴 高 | 事件重复发布 | 移除 `ETBattleView_EventHandler` 中的再次发布 |
| 🟡 中 | 静态字典 `_units` | 改为使用 Component 实例存储 |
| 🟡 中 | Component 包含 Log 调用 | 移除 Log.Info，移到 Handler |
| 🟢 低 | Attribute 优化 | 考虑使用 MarkerAttribute 模式 |

## 五、后续扩展建议

### 5.1 模块化拆分

当项目规模增大时，考虑按功能模块拆分：

```
ET.Demo.Logic/
├── Core/                    # 核心模块
│   ├── Battle/              # 战斗核心
│   ├── Unit/               # 单位管理
│   └── Input/              # 输入处理
│
├── Moba/                    # MOBA 特定逻辑
│   ├── Skills/             # 技能系统
│   ├── Items/              # 物品系统
│   └── Buffs/              # Buff 系统
│
└── Session/                 # 会话管理
    ├── Matchmaking/         # 匹配
    ├── Room/                # 房间
    └── Replay/              # 回放
```

### 5.2 配置驱动

使用 Attribute 配置替代硬编码：

```csharp
// 定义模块配置
[ModuleConfig("Battle", Priority = 1000)]
public class BattleModule : IModule
{
    [Component] public ETBattleComponent Battle { get; }
    [Handler] public List<Type> EventHandlers { get; }
}

// 模块初始化器
public interface IModuleInitializer
{
    void Initialize(Scene scene);
}

[ModuleInitializer]
public class BattleModuleInitializer : IModuleInitializer
{
    public void Initialize(Scene scene)
    {
        // 自动创建 Component
        // 自动注册 Handler
    }
}
```

## 六、总结

ET Demo 项目整体架构合理，但存在以下需要优化的地方：

1. **命名空间**：去掉多余的 `ET` 前缀
2. **事件流**：消除重复的事件发布
3. **数据存储**：使用 Component 实例替代静态字典
4. **代码组织**：按功能模块进一步拆分
5. **代码生成**：利用 Attribute 驱动减少样板代码

这些问题不影响当前功能，但会影响代码的可维护性和可扩展性。建议按优先级逐步优化。
