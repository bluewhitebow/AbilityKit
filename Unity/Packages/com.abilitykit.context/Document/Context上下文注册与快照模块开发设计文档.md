# Ability-Kit Context 上下文注册与快照模块开发设计文档

> **阅读对象**：需要理解 Ability-Kit 上下文数据容器、流程上下文、实体属性注册、快照存取机制的框架开发者。
>
> **文档目标**：说明 Context 模块解决什么问题、边界在哪里、核心类型如何协作，以及后续接入 Triggering、Trace、Combat、FrameSync 等模块时应注意哪些约束。

---

## 一、设计理念：为什么需要 Context 模块

Context 模块提供轻量的运行时上下文注册中心。它不试图替代完整 ECS，而是为技能、触发、战斗计算、快照回放等模块提供一套“按流程组织实体、按实体 ID 组织属性和快照”的基础能力。

典型痛点包括：

| 问题 | 具体表现 | Context 的处理方式 |
|------|----------|-------------------|
| 流程上下文散落 | 一次技能、一段命中、一轮结算缺少统一边界 | 用 `FlowContext` 表达 flow/session/scope |
| 临时上下文散落 | 技能释放者、目标、参数、运行状态散落在多个对象中 | 用 `ContextRegistry` 按实体 ID 管理属性集合 |
| 属性类型识别不统一 | 模块之间难以统一判断“是否具备某类数据” | 通过 `PropertyTypeRegistry` 为属性类型分配类型 ID |
| 快照追踪困难 | 临时实体销毁后仍需要回溯来源、Owner 或历史状态 | 用 `SnapshotStorage` 独立保存 `IContextSnapshot` |
| 溯源弱关联 | Context 与 Trace 直接互相依赖会导致包边界变重 | 用 `TraceContextProperty` 只保存 trace id |

核心思想是：Flow 表示一次流程边界，Entity 表示流程中的运行时对象，Property 表示对象能力或状态，Snapshot 表示可留存的历史视图。

---

## 二、模块边界

### 2.1 Context 负责什么

- 分配和维护上下文实体 ID。
- 创建和管理流程级 `FlowContext`，记录父子 flow、owner、阶段和包含的 entity。
- 为实体挂载、读取、覆盖、移除实现了 `IProperty` 的属性对象。
- 通过属性类型查询实体集合，并维护增量索引。
- 在属性、实体、flow 生命周期变化时派发事件。
- 保存实体快照，并按 `SourceEntityId`、`OwnerEntityId` 建立反向索引。
- 提供可选 `TraceContextProperty`，让业务把 trace root/context id 挂到 context entity 上。

### 2.2 Context 不负责什么

- 不负责系统调度，不会主动 Tick。
- 不负责组件序列化协议，只保存调用者传入的快照对象。
- 不负责 ECS 级别的 archetype、chunk、稀疏集合优化。
- 不负责属性对象的深拷贝和不可变性，需要调用者自行约束。
- 不负责 Unity 场景对象或 GameObject 生命周期。
- 不直接依赖 Trace 包；Trace 关联通过属性或适配器弱耦合完成。

---

## 三、目录结构

| 路径 | 职责 |
|------|------|
| `Runtime/Context/FlowContext.cs` | 流程上下文、流程阶段和作用域封装 |
| `Runtime/Context/TraceContextProperty.cs` | Trace id 弱关联属性和扩展方法 |
| `Runtime/Registry/ContextRegistry.cs` | 上下文实体、属性、flow 的注册中心 |
| `Runtime/Property/IProperty.cs` | 属性对象的基础标记接口 |
| `Runtime/Property/PropertyType.cs` | 属性类型描述和类型 ID 注册表 |
| `Runtime/Events/ContextEvent*.cs` | 上下文事件、事件类型、事件委托定义 |
| `Runtime/Query/Query.cs` | 基于属性类型的查询封装 |
| `Runtime/Snapshot/IContextSnapshot.cs` | 快照基础接口、版本快照接口和快照记录 |
| `Runtime/Snapshot/ISnapshotAccessor.cs` | 快照访问接口 |
| `Runtime/Snapshot/SnapshotStorage.cs` | 快照保存、查询、销毁标记和索引维护 |
| `Runtime/Internal/TimeUtil.cs` | 内部时间戳工具 |

---

## 四、核心类型与职责

### 4.1 FlowContext

`FlowContext` 表示一次流程上下文，例如一次技能释放、一次命中结算、一段状态机流程。它记录：

- `FlowId`：流程 ID。
- `ParentFlowId`：父流程 ID，可表达流程嵌套。
- `OwnerEntityId`：流程归属实体。
- `Phase`：`Created`、`Running`、`Completed`、`Cancelled`、`Failed`。
- `EntityIds`：挂在该流程下的上下文实体。
- `ChildFlowIds`：子流程。

`FlowContextScope` 适合用 `using` 包裹业务流程：进入时创建并置为 running，释放时按默认阶段完成，也可以显式 `Complete`、`Cancel` 或 `Fail`。

### 4.2 ContextRegistry

`ContextRegistry` 是本包的核心入口。内部维护：

- `_entities`：`entityId -> EntityData`，保存实体和属性字典。
- `_flows`：`flowId -> FlowContext`，保存流程上下文。
- `_entitiesByPropertyType`：属性类型到实体集合的增量索引。
- `_globalHandlers`：全局事件订阅者。
- `_idHandlers`：指定实体事件订阅者。
- `_lock`：保护实体、属性、flow 和订阅列表的同步锁。

对外能力：

| 方法 | 行为 |
|------|------|
| `Create()` | 创建无 flow 归属实体并返回 `EntityBuilder` |
| `CreateInFlow(flowId)` | 创建归属指定 flow 的实体 |
| `BeginFlow(...)` | 创建 flow scope，适合流程级上下文管理 |
| `SetFlowPhase(flowId, phase)` | 更新 flow 阶段并派发事件 |
| `Destroy(entityId)` | 派发 Destroying，移除实体和属性索引，派发 Destroyed |
| `Add<T>` / `Set<T>` | 注册属性类型 ID，写入属性，更新索引并派发 Updated |
| `Get<T>` / `Has<T>` | 按属性类型读取实体属性 |
| `Remove<T>` | 移除属性，更新索引并派发 Updated |
| `GetEntitiesWith<T>` | 查询拥有某类属性的实体 ID |
| `Query()` | 创建绑定当前 registry 的查询构建器 |
| `Clear()` | 逐个销毁当前实体，并清理 flow 与索引 |

事件处理器现在在锁外派发。注册表会先在锁内完成状态变更并复制订阅列表，然后释放锁再调用 handler，避免业务回调导致锁内重入和长时间阻塞。

### 4.3 Query

`Query` 支持属性存在与排除条件：

```csharp
var ids = registry.Query()
    .CreateQuery()
    .With<HealthProperty>()
    .Without<DeadProperty>()
    .Execute();
```

旧式 `query.Execute(registry)` 仍可使用，便于兼容已有调用。

### 4.4 SnapshotStorage

`SnapshotStorage` 独立于 `ContextRegistry`。它保存 `IContextSnapshot`，并根据快照是否实现额外接口建立索引：

| 接口 | 含义 | 索引/记录 |
|------|------|----------|
| `IContextSnapshot` | 快照基础信息，至少包含 `EntityId` | `_snapshots` |
| `IVersionedContextSnapshot` | 提供业务版本和帧号 | `ContextSnapshotRecord` |
| `ISourceContext` | 快照来源实体 | `_bySource` |
| `IOwnerContext` | 快照归属实体 | `_byOwner` |
| `IDestroyableSnapshot` | 可标记销毁状态 | `MarkDestroyed` |

重复保存同一实体快照时会先移除旧 source/owner 索引，再重建新索引，避免重复 ID 或旧索引残留。若快照未实现 `IVersionedContextSnapshot`，存储层会按 entity 自动递增版本。

### 4.5 TraceContextProperty

`TraceContextProperty` 是 Context 与 Trace 的弱桥接点。Context 包不引用 Trace 包，只保存：

- `RootTraceId`
- `TraceContextId`
- `TraceKind`

业务可以在创建 context entity 时调用 `WithTrace(rootId, contextId, kind)`，工具层或诊断层再按需把这些 id 关联到 Trace 注册表。

---

## 五、执行流程

### 5.1 创建流程和实体

```csharp
using (var flow = registry.BeginFlow("SkillCast", ownerEntityId: casterId))
{
    var entityId = flow.Create()
        .With(new HealthProperty())
        .WithTrace(rootTraceId, traceContextId)
        .Build();

    flow.Complete();
}
```

### 5.2 销毁实体

`Destroy(entityId)` 会先派发 Destroying，再从 `_entities` 移除实体、清理属性索引、从 flow 中解除绑定，最后派发 Destroyed。快照不会自动删除；如果需要保留历史状态，调用者应在销毁前保存快照，或者在销毁后调用 `SnapshotStorage.MarkDestroyed` 标记状态。

### 5.3 查询实体

`GetEntitiesWith<T>` 读取增量索引，不再每次扫描全部实体。`Query` 可以组合 With/Without 条件，适合常见的运行时筛选。

---

## 六、扩展点

- 新增上下文属性：实现 `IProperty`，直接挂载到实体。
- 新增流程语义：在业务层封装 `BeginFlow`，约定 flow name、owner 和 phase。
- 新增快照类型：实现 `IContextSnapshot`，需要版本/帧号时实现 `IVersionedContextSnapshot`。
- 订阅变更：通过 `Subscribe(handler)` 监听全局变化，或 `Subscribe(entityId, handler)` 监听指定实体。
- Trace 桥接：通过 `TraceContextProperty` 保存 trace id，不让 Context 直接依赖 Trace。

---

## 七、注意事项与当前限制

- `ContextRegistry` 的属性对象按引用保存，修改属性内部字段不会自动派发事件；需要通过 `Set` 重新写入才能通知。
- 事件已改为锁外派发，但 handler 抛异常仍会聚合为 `AggregateException` 抛给调用方。
- 快照和实体注册中心没有自动同步销毁关系，调用方需要显式保存、移除或标记快照。
- `FlowContext` 是流程组织模型，不负责驱动业务状态机，也不会自动 Tick。
- Query 支持 With/Without 与谓词过滤；OR、分组和值比较 DSL 可在后续按实际接入复杂度扩展。
- `FlowContext` 不内置 tag/category/faction 等业务分类；这些语义应通过业务侧 `IProperty`、业务查询服务或诊断适配层扩展。

---

## 八、最小接入示例

```csharp
public sealed class BusinessCategoryProperty : IProperty
{
    public BusinessCategoryProperty(string category) => Category = category;
    public int TypeId => PropertyTypeRegistry.Instance.Register<BusinessCategoryProperty>().Id;
    public string Category { get; }
}

var registry = new ContextRegistry();
using var flow = registry.BeginFlow("skill-effect", ownerEntityId: casterId);
var entityId = flow.Create()
    .WithTrace(rootTraceId, traceContextId)
    .With(new BusinessCategoryProperty("buff"))
    .Build();

var buffEntities = registry.Query()
    .CreateQuery()
    .With<BusinessCategoryProperty>()
    .Where<BusinessCategoryProperty>((_, property) => property.Category == "buff")
    .Execute();
```

接入方只需要把稳定身份、流程归属和通用 trace id 写入 Context；技能、Buff、阵营、标签、诊断分组等业务语义不要进入 Context 包，而是通过业务自定义属性或业务查询服务表达。

---

## 九、后续演进

- 为 Query 增加 OR、分组条件和值比较 DSL。
- 为业务诊断面板提供只读查询适配示例，而不是在 `FlowContext` 内置业务分类字段。
- 为快照提供序列化适配层和历史多版本存储。
- 增加只读属性访问或不可变属性约束，降低引用共享导致的状态不一致。

---

*文档版本：1.1*  
*最后更新：2026-06-17*
