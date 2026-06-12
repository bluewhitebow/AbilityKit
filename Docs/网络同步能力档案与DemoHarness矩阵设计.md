# 网络同步能力档案与 DemoHarness 矩阵设计

> 阅读对象：网络同步框架维护者、DemoHarness 维护者、Shooter / Moba 示例维护者。
>
> 文档目标：把《网络同步抽象审计与能力矩阵》的结论落成可执行 API 草案。本文暂不要求立刻修改 runtime 代码，而是定义下一轮实现时应遵循的能力档案、carrier capability、矩阵匹配结果和迁移步骤。

---

## 1. 设计结论

`NetworkSyncModel` 短期继续作为兼容档案名和旧入口存在，但不再扩张成“所有同步方案的单一枚举”。新的主抽象应是 `NetworkSyncProfile`：它描述一组可组合 policy，而不是选择一个互斥模式。

DemoHarness 也应从“按 `CarrierName + NetworkSyncModel` 找 carrier”升级为“按 `CarrierName + NetworkSyncProfile` 询问 carrier 是否支持”。不支持、部分支持、运行失败应成为矩阵的一等结果。

---

## 2. Policy 草案

### 2.1 ClientPlaybackPolicy

```csharp
public enum ClientPlaybackPolicy
{
    None = 0,
    PredictRollback = 1,
    AuthoritativeInterpolation = 2,
    HoldLatest = 3,
    ExtrapolateThenCorrect = 4,
    HybridLocalPredictRemoteInterpolate = 5
}
```

说明：

- `PredictRollback` 和 `AuthoritativeInterpolation` 是已经落地的客户端能力。
- `HybridLocalPredictRemoteInterpolate` 是组合档案的便捷描述，落地时仍应拆到不同对象/实体类型。
- `ExtrapolateThenCorrect` 当前没有实现；现有插值播放刻意保持最新权威姿态，不编造运动。

### 2.2 InputPolicy

```csharp
[Flags]
public enum InputPolicy
{
    None = 0,
    NoClientInput = 1 << 0,
    ImmediateSubmit = 1 << 1,
    InputDelayBuffer = 1 << 2,
    ServerRemapAcceptedFrame = 1 << 3,
    DeterministicBroadcast = 1 << 4
}
```

说明：

- Shooter 当前至少具备 `ImmediateSubmit | ServerRemapAcceptedFrame` 的语义基础。
- Moba 当前同步演示偏远端播放，输入策略还没有进入通用同步框架。
- `DeterministicBroadcast` 属于 lockstep，不应和普通服务器权威状态同步混写。

### 2.3 SnapshotPolicy

```csharp
[Flags]
public enum SnapshotPolicy
{
    None = 0,
    FullSnapshot = 1 << 0,
    DeltaSnapshot = 1 << 1,
    KeyFrameSnapshot = 1 << 2,
    AuthorityOverride = 1 << 3,
    FixedRateStateStream = 1 << 4,
    BatchSnapshot = 1 << 5,
    EventStream = 1 << 6
}
```

说明：

- `FullSnapshot`、`AuthorityOverride`、`FixedRateStateStream` 已有可复用基础。
- `BatchSnapshot`、`EventStream` 还只是方向，应先在文档和 harness 里成为可声明能力，再落实现。

### 2.4 InterestPolicy

```csharp
[Flags]
public enum InterestPolicy
{
    None = 0,
    AllEntities = 1 << 0,
    OwnerRelevant = 1 << 1,
    DistanceAoi = 1 << 2,
    TeamOrFactionAoi = 1 << 3,
    PriorityBudget = 1 << 4,
    LodFrequency = 1 << 5
}
```

说明：

- 当前 Shooter / Moba 小规模演示基本等价于 `AllEntities`。
- 大规模战场能力的核心是 `DistanceAoi | PriorityBudget | LodFrequency`，不是一个新的客户端 controller。

### 2.5 RecoveryPolicy

```csharp
[Flags]
public enum RecoveryPolicy
{
    None = 0,
    RequestFullSnapshot = 1 << 0,
    RequestKeyFrame = 1 << 1,
    RequestAoiSlice = 1 << 2,
    CatchUpToServerFrame = 1 << 3,
    ReconnectResume = 1 << 4
}
```

说明：

- `FastReconnect` 应落在 `ReconnectResume`，不是和 `PredictRollback` 平级的模式。
- `RequestAoiSlice` 依赖 AOI/兴趣管理能力，不能单独成立。

### 2.6 ServerValidationPolicy

```csharp
[Flags]
public enum ServerValidationPolicy
{
    None = 0,
    AuthoritativeOnly = 1 << 0,
    InputValidation = 1 << 1,
    LagCompensatedHitValidation = 1 << 2,
    ClientHashAudit = 1 << 3,
    AntiCheatEnvelope = 1 << 4
}
```

说明：

- `LagCompensatedHitValidation` 是服务器侧能力，可以和预测、插值、批量快照组合。
- DemoHarness 需要知道场景是否有命中请求、历史帧、hitbox 语义，否则不能把该能力标成 supported。

---

## 3. NetworkSyncProfile 草案

```csharp
public readonly struct NetworkSyncProfile
{
    public NetworkSyncProfile(
        NetworkSyncModel compatibilityModel,
        ClientPlaybackPolicy clientPlayback,
        InputPolicy input,
        SnapshotPolicy snapshot,
        InterestPolicy interest,
        RecoveryPolicy recovery,
        ServerValidationPolicy serverValidation)
    {
        CompatibilityModel = compatibilityModel;
        ClientPlayback = clientPlayback;
        Input = input;
        Snapshot = snapshot;
        Interest = interest;
        Recovery = recovery;
        ServerValidation = serverValidation;
    }

    public NetworkSyncModel CompatibilityModel { get; }
    public ClientPlaybackPolicy ClientPlayback { get; }
    public InputPolicy Input { get; }
    public SnapshotPolicy Snapshot { get; }
    public InterestPolicy Interest { get; }
    public RecoveryPolicy Recovery { get; }
    public ServerValidationPolicy ServerValidation { get; }
}
```

推荐提供内置 profile 工厂，避免测试和文档里手写一堆 policy：

```csharp
public static class NetworkSyncProfiles
{
    public static NetworkSyncProfile PredictRollback { get; }
    public static NetworkSyncProfile AuthoritativeInterpolation { get; }
    public static NetworkSyncProfile HybridHeroPrediction { get; }
    public static NetworkSyncProfile MassBattleLodSync { get; }
}
```

`CompatibilityModel` 只用于旧工厂、旧 scenario、日志和迁移期比较。新逻辑判断支持能力时应读各 policy 字段。

---

## 4. Carrier Capability 草案

### 4.1 支持结果

```csharp
public enum SyncDemoCapabilityStatus
{
    Supported = 0,
    Unsupported = 1,
    Degraded = 2
}

public readonly struct SyncDemoCapabilityResult
{
    public SyncDemoCapabilityResult(SyncDemoCapabilityStatus status, string reason)
    {
        Status = status;
        Reason = reason ?? string.Empty;
    }

    public SyncDemoCapabilityStatus Status { get; }
    public string Reason { get; }
    public bool CanRun => Status == SyncDemoCapabilityStatus.Supported || Status == SyncDemoCapabilityStatus.Degraded;
}
```

含义：

- `Supported`：能力完整支持，可正常进入 `Run`。
- `Degraded`：可运行但能力降级，例如没有真实带宽预算，仅能收集基础网络统计。
- `Unsupported`：不能运行，例如 Moba carrier 被要求跑 `PredictRollback`。

### 4.2 Carrier 声明接口

短期不要破坏现有 `ISyncDemoCarrier`，建议新增可选接口：

```csharp
public interface ISyncDemoCarrierCapabilities
{
    SyncDemoCapabilityResult Supports(in NetworkSyncProfile profile, in NetworkConditionProfile networkProfile);
}
```

现有 carrier 可以逐步实现该接口。未实现时，DemoHarness 走兼容路径：仍按 `CarrierName + SyncModel` 匹配，并把能力结果视为 legacy supported。

### 4.3 示例能力声明

Shooter PredictRollback carrier：

```csharp
public SyncDemoCapabilityResult Supports(in NetworkSyncProfile profile, in NetworkConditionProfile networkProfile)
{
    if (profile.ClientPlayback != ClientPlaybackPolicy.PredictRollback)
    {
        return new SyncDemoCapabilityResult(SyncDemoCapabilityStatus.Unsupported, "Shooter rollback carrier only supports PredictRollback playback.");
    }

    if ((profile.Snapshot & SnapshotPolicy.FullSnapshot) == 0)
    {
        return new SyncDemoCapabilityResult(SyncDemoCapabilityStatus.Unsupported, "PredictRollback requires full or authority override snapshots.");
    }

    return new SyncDemoCapabilityResult(SyncDemoCapabilityStatus.Supported, string.Empty);
}
```

Moba AuthoritativeInterpolation carrier：

```csharp
public SyncDemoCapabilityResult Supports(in NetworkSyncProfile profile, in NetworkConditionProfile networkProfile)
{
    if (profile.ClientPlayback != ClientPlaybackPolicy.AuthoritativeInterpolation)
    {
        return new SyncDemoCapabilityResult(SyncDemoCapabilityStatus.Unsupported, "Moba carrier currently supports authoritative interpolation only.");
    }

    if ((profile.Interest & InterestPolicy.AllEntities) == 0)
    {
        return new SyncDemoCapabilityResult(SyncDemoCapabilityStatus.Degraded, "Moba carrier does not yet implement AOI; running as all-entities playback.");
    }

    return new SyncDemoCapabilityResult(SyncDemoCapabilityStatus.Supported, string.Empty);
}
```

---

## 5. DemoHarness 迁移草案

### 5.1 Scenario 扩展

短期保留现有构造函数，新增 profile 构造或属性：

```csharp
public readonly struct DemoHarnessScenario
{
    public NetworkSyncModel SyncModel { get; }
    public NetworkSyncProfile SyncProfile { get; }
    public NetworkConditionProfile NetworkProfile { get; }
    public string CarrierName { get; }
}
```

兼容规则：

- 旧构造函数传入 `NetworkSyncModel` 时，通过 `NetworkSyncProfiles.FromCompatibilityModel(syncModel)` 生成 `SyncProfile`。
- 新构造函数直接传入 `NetworkSyncProfile`。
- `SyncModel` 仍返回 `SyncProfile.CompatibilityModel`，保持旧测试可过。

### 5.2 RunResult 扩展

现有 `DemoHarnessRunResult.Completed + FailureReason` 只能表达成功/失败。能力矩阵需要更细：

```csharp
public enum DemoHarnessRunStatus
{
    Completed = 0,
    Unsupported = 1,
    Degraded = 2,
    Failed = 3
}
```

推荐迁移：

- 短期新增 `Status` 属性，`Completed` 仍根据 `Status == Completed || Status == Degraded` 兼容返回。
- `Unsupported` 不应算运行失败，而是矩阵结果。
- `Failed` 表示支持声明通过后，实际运行中 carrier 或 telemetry 出错。

### 5.3 Runner 匹配流程

新的 `RunMany` 逻辑应变为：

1. 按 `CarrierName` 找候选 carrier。
2. 若 carrier 实现 `ISyncDemoCarrierCapabilities`，调用 `Supports(profile, networkProfile)`。
3. 若返回 `Unsupported`，记录 unsupported result，继续后续 scenario。
4. 若返回 `Degraded`，运行场景，但 result 标记 degraded，并保留 reason。
5. 若 carrier 未实现 capability 接口，回退到旧的 `carrier.SyncModel == scenario.SyncModel`。
6. 单场景 `Run` 也执行同样验证，避免绕过 `RunMany`。

---

## 6. 分阶段实现建议

### 阶段 1：文档与类型草案

- 新增 `NetworkSyncProfile` 与 policy enum 设计，不改 runtime。
- 更新现有文档，把 `NetworkSyncModel` 统一写成兼容档案名。
- 明确 `IClientSyncStrategy<TInput,TSample>` 只是客户端策略接口。

### 阶段 2：最小 runtime 类型

- 在 `network.runtime/Runtime/Network/Runtime/Sync` 新增 policy enum 与 `NetworkSyncProfile`。
- 新增 `NetworkSyncProfiles` 静态工厂。
- 不修改现有 carrier 行为。

### 阶段 3：DemoHarness 支持 capability

- 新增 `ISyncDemoCarrierCapabilities`、`SyncDemoCapabilityResult`、`DemoHarnessRunStatus`。
- `DemoHarnessRunner` 保留旧匹配逻辑作为 fallback。
- 单测覆盖 supported / unsupported / degraded / legacy fallback。

### 阶段 4：真实 carrier 声明能力

- Shooter carrier 声明 PredictRollback 或 AuthoritativeInterpolation 能力。
- Moba carrier 声明 AuthoritativeInterpolation 能力。
- 对缺失 AOI、BatchSnapshot、FastReconnect 的场景返回 unsupported 或 degraded。

### 阶段 5：矩阵报告与 UI

- `DemoHarnessBatchResult` 统计 completed / unsupported / degraded / failed。
- 可视化层把 unsupported 显示为能力缺口，而不是错误。
- 后续再把 sync event / health 接入指标。

---

## 7. 验收标准

下一轮实现完成时，应满足：

- 旧的 `NetworkSyncModel` scenario 和测试继续可运行。
- 新的 `NetworkSyncProfile` scenario 可以声明组合能力。
- carrier 能解释自己为什么不支持某个 profile。
- DemoHarness 批量矩阵不会因为一个 unsupported 组合中断。
- `HybridHeroPrediction`、`MassBattleLodSync`、`FastReconnect` 不再被迫当成单一客户端 controller。

---

## 8. 结论

这一步的关键不是多写几个 enum，而是把 DemoHarness 的判断从“名字是否相等”升级为“能力是否满足”。只要这个边界稳定，后续接 BatchSnapshot、AOI/LOD、FastReconnect、LagCompensation 时，就不会继续把可组合能力塞回一个互斥枚举里。
