# Shooter 协议与战斗流程阶段性总结

## 阶段结论

当前 Shooter 示例的协议设计和战斗流程整体符合预期，可以作为后续继续扩展前的阶段性基线。

目前已经形成了三层清晰边界：

- Room Gateway 协议负责账号会话、房间生命周期、战斗启动入口、状态同步订阅入口和通用输入转发。
- Orleans Battle Host 负责战斗生命周期、固定帧驱动、输入调度、观察者管理和权威快照发布。
- Shooter 玩法层负责玩法 payload、packed snapshot、客户端预测、权威快照覆盖、未确认输入重放和表现投影。

这个方向是合理的。它避免了 Gateway 直接理解 Shooter 输入和状态细节，也避免了 Shooter 客户端绕过统一房间协议直接绑定战斗 Grain。后续大改时应优先保护这条边界。

## 当前协议边界

### Room Gateway 通用协议

Room Gateway 当前承载的是跨玩法通用协议：

- `WireRoomGuestLoginReq` / `WireRoomGuestLoginRes`：建立 guest account 和 session token。
- `WireCreateRoomReq` / `WireCreateRoomRes`：创建房间，包含 region、serverId、roomType、title、maxPlayers、tags。
- `WireJoinRoomReq` / `WireJoinRoomRes`：加入房间或进入运行中战斗，返回 JoinKind、WorldStartAnchor、ServerNowTicks 等同步锚点。
- `WireRoomReadyReq` / `WireRoomSnapshotRes`：修改房间准备状态并返回房间快照。
- `WireStartRoomBattleReq` / `WireStartRoomBattleRes`：启动战斗，返回 BattleId、WorldId、WorldStartAnchor、ServerNowTicks。
- `WireSubscribeStateSyncReq` / `WireSubscribeStateSyncRes`：订阅某个 Battle 的状态同步推送。
- `WireSubmitBattleInputReq` / `WireSubmitBattleInputRes`：提交玩法输入 payload，并返回 AcceptedFrame、CurrentFrame、Status、ShouldResync、ServerTicks。
- `WireStateSyncSnapshotPush`：状态同步推送外壳，包含 WorldId、Frame、IsFullSnapshot、PayloadOpCode、Payload、ServerTicks。

这里的设计重点是：Room Gateway 只知道 payload 的路由和诊断字段，不理解 Shooter 的输入二进制含义，也不理解 packed snapshot 内部结构。

### Shooter 玩法协议

Shooter 玩法协议当前通过 `PayloadOpCode` 和二进制 payload 插入通用 Room Gateway 外壳中：

- Shooter 输入通过 `WireSubmitBattleInputReq.InputOpCode` 和 `Payload` 承载。
- Shooter full packed snapshot 使用 `ShooterOpCodes.Snapshot.PackedState`。
- Shooter delta packed snapshot 使用 `ShooterOpCodes.Snapshot.PackedStateDelta`。
- Room Gateway push opCode 区分 full 与 delta：`SnapshotPushed` 表示完整推送，`DeltaSnapshotPushed` 表示增量推送。
- `ShooterPackedSnapshotPayload` 内部保留 WorldId、Frame、ServerTick、SnapshotFlags、StateHash、EntityCount、ExtensionPayload、ComponentChunks。

这一层的现状是合理的：Gateway push opCode 表达通用同步语义，Shooter payload opCode 表达玩法负载语义，二者没有互相替代。

## 标准战斗流程

### 创建并启动新战斗

标准新战斗路径是：

1. Guest login 获取 `SessionToken` 和 `AccountId`。
2. Create room 创建 `roomType=shooter` 的房间。
3. Join room 让创建者进入房间。
4. Set ready 更新准备状态。
5. Start battle 将房间玩家/loadout 映射为 `BattleInitParams`。
6. BattleLogicHost 初始化 Shooter runtime session，生成 `WorldStartAnchor`，启动固定帧 Tick。
7. Subscribe state sync 绑定账号到 Gateway connection，并订阅 `StateSyncObserverGrain`。
8. 服务端推送 full snapshot。
9. 客户端用 start anchor 启动 Shooter session，追到 TargetFrame。
10. 客户端提交输入，服务端按 frame scheduler 接受、重映射或拒绝。
11. 服务端持续推送 authority snapshot，客户端导入、校正、重放未确认输入并更新表现。

当前实现符合这个流程。

### 加入未开战房间

加入未开战房间时，客户端不创建房间，只走：

1. Join room。
2. Set ready。
3. Start battle。
4. Subscribe state sync。
5. 后续同新战斗路径。

当前单测已覆盖该路径不会调用 create，并且能正确建立 BattleId、WorldId、TargetFrame 和输入上下文。

### Late Join 运行中战斗

晚进运行中战斗时，Join room 返回 `WireRoomJoinKind.LateJoin`，并带已有 BattleId、WorldId、WorldStartAnchor、ServerNowTicks。客户端流程是：

1. Join room。
2. 发现 JoinKind 不是 TeamLobby 且 BattleId 非空。
3. 跳过 SetReady 和 StartBattle。
4. 直接 Subscribe state sync。
5. 使用 Join 返回的 anchor 计算 TargetFrame。
6. 启动本地 Shooter session 并 catch up。
7. 等待 full snapshot 或 actor snapshot 应用到 runtime/presentation/projection。

当前单测和 smoke 都覆盖了该路径。这个分支设计符合预期，因为运行中战斗不能重新 ready/start。

### Reconnect 运行中战斗

重连运行中战斗与 Late Join 类似，但 JoinKind 为 `Reconnect`。关键差异是同账号可能复用同一个 `StateSyncObserverGrain` key，因此重复订阅不能被简单忽略。

当前设计中，重复订阅会触发 targeted full snapshot：

- `StateSyncObserverGrain` 识别 duplicate subscription。
- 调用 `BattleLogicHostGrain.RequestFullSnapshotAsync`。
- `BattleSnapshotPublisher.PublishTo` 对单个 observer 推送 full snapshot。

这个设计符合预期，解决了重连时已经订阅但新连接收不到 fresh full snapshot 的问题。

## 同步与校正语义

### 时间锚点

当前时间锚点已经统一为 server ticks 语义：

- Start battle 返回 `WorldStartAnchor` 和 `ServerNowTicks`。
- Join running battle 返回同一局的 `WorldStartAnchor` 和当前 `ServerNowTicks`。
- Snapshot push 返回 `ServerTicks`。
- Input response 返回 `ServerTicks`。
- 客户端通过 anchor 计算 `TargetFrame` 和 `CatchUpFrames`。

这条线目前符合预期。后续应避免重新引入不明确的小数秒时间语义；`Timestamp` 可以继续作为兼容字段，但新逻辑应优先使用 `ServerTicks`。

### 输入提交

输入提交当前是玩法无关路由：

- Gateway 校验 session token、BattleId、WorldId、Frame、PlayerId。
- Gateway 将 `InputOpCode` 和 `Payload` 包装为 `BattleInputItem`。
- Battle Host 校验 WorldId，按 `BattleInputFrameScheduler` 计算 accepted frame。
- 响应返回 Success、AcceptedFrame、CurrentFrame、Status、ShouldResync、ServerTicks。

这个设计符合预期。Gateway 不解释 Shooter command，Battle Host 也只处理通用输入调度，具体输入 payload 由 Shooter runtime adapter/session 消费。

### Snapshot 推送

当前 snapshot 推送分为两层语义：

- Gateway opCode：full snapshot push 或 delta snapshot push。
- PayloadOpCode：Shooter packed state 或 Shooter packed delta。

客户端接收后通过 `ShooterRoomGatewayConnection` 分发到 `ShooterClientSession.ApplyGatewayPush`，再由 `ShooterPackedSnapshotSyncController` 解码和应用。

当前 packed full snapshot 应用语义是：

- 如果 snapshot frame 不大于上次已应用 frame，则返回 `IgnoredStaleSnapshot`。
- 如果 payload 不含 packed snapshot，则只更新表现侧 actor snapshot。
- 如果 payload 含 packed snapshot，则导入 Shooter runtime，更新 LastAppliedFrame、LastAppliedStateHash，并发布表现 snapshot。

这个语义符合预期。尤其是旧快照忽略机制，是后续预测回滚能稳定工作的必要边界。

### 客户端预测与重放

当前客户端校正路径已经具备以下能力：

- 本地 session 启动后追到 flow target frame。
- 本地输入按当前 frame 提交到 Gateway。
- 收到 authority packed snapshot 后覆盖本地 runtime。
- 裁剪已确认输入，重放未确认输入。
- 对旧 authority snapshot 返回 `IgnoredStaleSnapshot`，避免本地帧回退。
- 表现侧 projection 能处理 full batch 和 delta batch。

这条路径当前符合预期，可以继续作为 Shooter 同步示例的核心价值点。

## 已有测试与 Smoke 覆盖

### 单元测试覆盖

当前单元测试覆盖面已经比较完整，主要包括：

- Room Gateway flow：创建/加入/ready/start/subscribe、已有房间加入、late join、reconnect。
- Client Gateway launcher：启动 session、anchor 注入、catch up、late join/reconnect 跳过 ready/start、输入上下文。
- Room Gateway room client：使用通用房间生命周期协议。
- Gateway connection：请求响应和 snapshot push 分发。
- Input response wire：CurrentFrame、Status、ShouldResync、ServerTicks round trip。
- WorldStartFrameCatchUpCalculator：基于 anchor 计算 target frame 和 catch up frame。
- BattleInputFrameScheduler：晚到输入重映射、过早输入重映射、过远未来输入拒绝。
- Shooter packed snapshot runtime：导出/导入后恢复 hash 和实体。
- Wire snapshot push：Room 外壳保留 Shooter packed payload。
- Packed snapshot sync controller：authority snapshot 覆盖 runtime 和 view model。
- Client frame sync controller：authority overwrite 后继续 tick、重放 pending inputs、丢帧后 reconcile、rollback buffer 恢复、旧 authority snapshot 忽略。
- Presentation projection：full snapshot 替换、delta 更新、缺失实体移除、本地预测 full snapshot 不误删、sink 发布。
- Rollback command log：副作用补偿型 rollback 基础能力。
- Shooter world module：运行端口、Svelto 服务、world host 驱动。

这说明当前不是只靠 smoke 撑信心，核心协议和同步算法都有可定位的单测保护。

### Smoke 覆盖

当前 Shooter TCP Gateway smoke 覆盖端到端链路：

- Orleans local silo 启动。
- TCP Gateway 启动。
- GameFramework network channel wrapper 接入。
- Guest login。
- Create room / join / ready / start / subscribe。
- WorldId、BattleId、WorldStartAnchor、ServerNowTicks 校验。
- 等待 full packed snapshot push。
- 校验 `WireStateSyncSnapshotPush.ServerTicks > 0`。
- 校验 `PayloadOpCode == ShooterOpCodes.Snapshot.PackedState`。
- 解码 `ShooterPackedSnapshotPayload`。
- 校验 wire/packed WorldId 和 Frame 一致。
- 校验 packed ServerTick、StateHash、EntityCount。
- 校验 snapshot 应用后 runtime/presentation 对齐。
- 连续提交多帧输入并校验响应诊断字段。
- 构造 stale snapshot 并确认返回 `IgnoredStaleSnapshot`。
- 校验 primary client projection。
- 校验 late join projection。
- 校验 reconnect projection，包含同账号 targeted full snapshot。

这条 smoke 已经足够作为当前阶段的端到端回归闸门。

## 当前应冻结的基线

后续大改前，建议把以下点视为阶段性冻结边界：

1. Gateway 不解释玩法 payload，只做鉴权、路由、通用输入诊断和 push 分发。
2. Room 协议继续作为客户端进入战斗的唯一公开入口，不让 Shooter 客户端直接依赖 Orleans Grain。
3. `BattleId` 用于输入和订阅路由，`WorldId` 用于校验逻辑世界身份。
4. `WorldStartAnchor`、`ServerNowTicks`、snapshot `ServerTicks`、input response `ServerTicks` 保持同一 server ticks 时间域。
5. JoinKind 三态语义保持稳定：TeamLobby、LateJoin、Reconnect。
6. LateJoin/Reconnect 运行中战斗必须跳过 ready/start，直接订阅状态同步。
7. 重复订阅同一 observer 必须触发 targeted full snapshot，不能静默忽略。
8. Snapshot push 外层 opCode 和 Shooter payload opCode 分层表达语义。
9. Full packed snapshot 可以覆盖 runtime；旧 frame snapshot 必须被忽略，不能回退客户端。
10. 输入响应必须保留 CurrentFrame、Status、ShouldResync、ServerTicks，作为后续重同步策略的诊断基础。

## 后续大改前需要补齐或警惕的点

### 协议版本与兼容策略

当前 MemoryPack 字段主要通过追加字段演进。后续如果要继续扩展 wire 类型，建议明确协议版本策略：

- 哪些字段允许追加。
- 哪些字段进入后不可改序号。
- 哪些字段需要默认值兼容旧客户端。
- Shooter payload 的 Version 与 Room wire 的兼容边界如何协同。

### Delta snapshot 语义

当前 smoke 会跳过 delta push，只捕获 full packed snapshot 做强校验。delta snapshot 路径已有投影和同步基础，但还应补一条更明确的端到端验证：

- 服务端发出 delta packed payload。
- 客户端按 delta payload 应用或合并。
- projection 不误删缺失实体。
- 遇到 gap 或 import failure 时能要求 full resync。

### ShouldResync 后续策略

当前 input response 已有 `ShouldResync`，但 smoke 主路径仍期望输入成功。后续可以增加拒绝输入场景：

- 过远未来帧被拒绝。
- response 返回 `ShouldResync=true`。
- 客户端等待下一帧 full authority snapshot。
- 导入后恢复可提交输入状态。

### 多玩家真实输入归属

当前 smoke 已经覆盖 late join/reconnect，但 primary 输入仍是单客户端主导。后续如果要验证更接近实战的多人场景，应补：

- 两个不同 account 同时提交输入。
- server snapshot 同时包含两个玩家状态变化。
- late join 后能看到已有玩家和运行中子弹。
- reconnect 后同账号不重复生成玩家实体。

### Full build 阻塞项

当前 ShooterSmoke 自身 `--no-dependencies` build 和 smoke run 已通过，但完整依赖 build 被无关 Moba 代码阻塞：

```text
Unity/Packages/com.abilitykit.demo.moba.runtime/Runtime/Application/Services/Skill/Cast/SkillExecutor.cs(376,13): error CS0103: 当前上下文中不存在名称“_castSequenceByActor”
```

这个问题不影响本阶段 Shooter 协议结论，但会影响全仓级 CI 信心。后续进入更大范围改造前，建议先恢复完整 build 绿色。

## 阶段判断

当前协议设计和战斗流程可以继续沿用，不建议推倒重来。

更准确地说，当前应该进入“冻结主干语义，补齐边缘策略”的阶段：

- 主干语义：Room Gateway lifecycle、WorldStartAnchor、BattleId/WorldId、input response diagnostics、full snapshot authority override、late join/reconnect targeted full snapshot，都已符合预期。
- 需要补齐：delta snapshot 端到端策略、ShouldResync 自动恢复、多玩家输入归属、协议版本兼容说明。
- 大改原则：后续重构可以优化内部实现，但不应破坏上面列出的冻结边界，除非先更新文档并同步改测试和 smoke 验收线。
