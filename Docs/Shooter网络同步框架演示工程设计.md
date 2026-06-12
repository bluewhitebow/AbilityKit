# Shooter 网络同步框架演示工程设计

## 背景

Shooter 示例最初用于验证 AbilityKit 在一个轻量玩法中的完整链路：Room Gateway、Orleans Battle Host、Shooter 玩法协议、客户端预测、权威快照、回滚重放和表现投影。

随着当前双端帧同步链路逐步补齐，新的问题也变得明确：当单位数量非常多、状态变化密集、权威快照频繁到达时，客户端回滚和重放会非常频繁。对于小规模强一致玩法，这种模式有价值；但对于大规模战场、远端玩家插值、低频批量状态同步等场景，继续把双端帧同步作为 Shooter 示例的唯一定位并不合适。

因此 Shooter 示例的新定位应该从“一个双端帧同步 Shooter Demo”升级为“网络同步方案框架演示工程”。它的核心价值不再是证明某一种同步方案成立，而是展示 AbilityKit 如何在同一套房间、战斗、网关、时间锚点、快照和表现基础设施上，切换并组合多种网络游戏同步策略。

## 新定位

> 抽象校准：本文早期使用“同步模式”描述 Shooter 演示方向，这在阶段推进时是有效的工作语言；但按后续审计结论，`PredictRollback` / `AuthoritativeInterpolation` / `BatchStateSync` / `MassBattleLodSync` / `FastReconnect` / `ServerRewindLagCompensation` 并不都处在同一抽象层。后续应以《网络同步抽象审计与能力矩阵》的“同步能力档案”作为最终口径，本文中的“同步模式”除非特指历史枚举，否则都应理解为一组可组合能力的演示档案。

Shooter 示例用于演示多类网络同步能力档案在同一玩法场景下的工程落地方式，包括但不限于：

- 双端帧同步：输入同步、客户端预测、服务器权威快照、回滚重放、漂移检测和强同步恢复。
- 服务器权威状态同步：服务端按固定频率下发实体状态，客户端使用插值、外推和平滑纠正。
- 守望先锋式混合同步：本地玩家预测，远端实体插值，关键状态由服务器权威校正。
- 低频批量快照同步：服务端每 5 到 10 帧下发一批数据，客户端基于插值缓冲消费，适合大量单位。
- 大规模红蓝战场同步：服务器权威、兴趣管理、实体分区、LOD 同步、批量快照和关键事件优先广播。
- 快速重连：客户端携带世界标识、最后确认帧、快照版本或状态哈希，服务端补发关键快照并恢复订阅。
- 混合模式：本地英雄高频预测，远端玩家插值，普通单位低频批量同步，技能事件或投射物按事件流同步。

Shooter 玩法本身应该保持简单，避免被复杂技能、配置和表现细节淹没。它要成为网络同步策略的可视化样板，而不是另一个复杂玩法 Demo。

## 设计原则

### 保留现有主干边界

当前已经形成的三层边界仍然成立，后续不应轻易打破：

- Room Gateway 负责账号会话、房间生命周期、战斗启动入口、状态同步订阅入口、通用输入转发和 push 分发。
- Orleans Battle Host 负责战斗生命周期、固定帧驱动、输入调度、观察者管理和权威快照发布。
- Shooter 玩法层负责玩法输入、状态编码、同步模式策略、客户端预测、插值缓冲、表现投影和恢复决策。

Gateway 不解释 Shooter 的输入 payload 和快照 payload。Shooter 客户端也不直接依赖 Orleans Grain，而是通过统一 Room Gateway 协议进入战斗。

### 同一玩法，多种同步能力档案

Shooter 应该支持用同一个战斗基础模型运行多种同步能力组合。不同档案之间共享玩家、子弹、单位、阵营、世界时间和基础快照模型，但客户端播放策略、输入策略、服务端快照发布策略、兴趣管理、恢复流程和服务器侧判定能力可以不同。

同步能力档案应成为显式配置，而不是散落在不同 launcher 或 controller 里的分支。例如房间创建或启动战斗时可以声明一个兼容档案名：

- `SyncProfile=PredictRollback`
- `SyncProfile=AuthoritativeInterpolation`
- `SyncProfile=BatchStateSync`
- `SyncProfile=MassBattleLodSync`
- `SyncProfile=HybridHeroPrediction`

第一阶段可以先在 Shooter 内部使用 `NetworkSyncModel` 或本地配置作为兼容入口，后续再协议化到 Room tags、BattleInitParams 或玩法启动参数。这里的 profile 名称不代表底层互斥枚举，最终应展开为客户端播放、输入、快照、兴趣管理、恢复和服务器判定等 policy。

### 服务端权威是共同底座

无论客户端是否预测，服务端都应该是最终权威。不同模式的区别在于：

- 客户端是否提前模拟本地输入。
- 客户端是否回滚并重放。
- 服务端快照下发频率。
- 客户端如何消费快照。
- 哪些实体属于高频同步，哪些实体属于低频同步。
- 出现漂移、丢包、超时、重连时如何恢复。

### 演示要可对比

这个工程应允许开发者观察不同策略的差异，而不是只看代码结构。后续可以增加同步诊断面板或日志指标：

- 当前同步模式。
- RTT、jitter、丢包模拟参数。
- 服务端当前帧与客户端表现帧。
- 插值缓冲长度。
- 每秒快照包数量与字节数。
- 本地预测帧数、回滚次数、重放输入数。
- full snapshot、delta snapshot、batch snapshot 数量。
- resync 次数、原因和恢复耗时。

## 同步能力矩阵

| 档案或能力 | 抽象层级 | 适用场景 | 服务端侧重点 | 客户端侧重点 | 风险 |
| --- | --- | --- | --- | --- | --- |
| PredictRollback | 客户端播放/对账策略 | 小规模强交互射击、格斗、动作验证 | 高频权威快照或关键帧 | 本地预测、收到权威后覆盖并重放 | 单位多时回滚成本高 |
| AuthoritativeInterpolation | 客户端远端播放策略 | 中小规模在线射击、远端玩家表现 | 固定频率实体状态流 | 远端插值/短期保持，通常可与本地玩家预测组合 | 需要处理插值延迟和纠正 |
| BatchStateSync | 快照发布策略 | 大量普通单位、弱交互对象 | 每 5 到 10 帧批量下发状态 | 延迟播放、插值消费 | 近身交互需要额外高频通道 |
| MassBattleLodSync | 兴趣管理/带宽策略 | 大规模红蓝战场 | 分区、兴趣管理、LOD 快照 | 只消费可见或相关实体，低优先级对象低频更新 | 需要复杂筛选和优先级系统 |
| HybridHeroPrediction | 组合档案 | MOBA/英雄射击/载具战斗 | 英雄高频，普通单位低频，事件单独广播 | 自己预测，其他关键目标插值，普通单位批量同步 | 组合复杂，必须有能力声明和诊断 |
| FastReconnect | 恢复流程 | 断线重连、切后台恢复 | targeted full snapshot、keyframe 或 AOI slice 补发 | 快速恢复世界、表现和输入上下文 | 需要稳定的 world/session identity |

## 建议架构分层

### 同步能力档案层

新增或沉淀一组同步能力档案抽象，用于描述一局 Shooter 战斗启用哪些可组合能力，而不是只选择一个互斥模式。

建议职责：

- 声明兼容档案名和能力标记。
- 声明客户端播放/对账策略。
- 声明输入提交和预测策略。
- 声明服务端快照发布策略。
- 声明兴趣管理和带宽策略。
- 声明恢复策略。
- 声明服务器侧判定能力。
- 提供诊断指标和支持矩阵。

早期候选的 `ShooterSyncMode` / `IShooterSyncModeStrategy` 可以视为历史草案：它适合帮助第一阶段完成 controller 分流，但不适合作为最终抽象。最终口径应靠框架层的能力档案表达，Shooter 只保留玩法适配和 profile alias。

### 服务端快照策略层

服务端需要从“每帧或固定频率发布一种 packed snapshot”升级为可配置策略：

- 高频 full/keyframe snapshot。
- delta snapshot。
- 每 N 帧批量 snapshot。
- 按实体类型分层下发。
- 按 AOI/兴趣管理筛选实体。
- 关键事件立即广播，普通状态延迟批量。
- targeted full snapshot 用于重连和强恢复。

候选能力：

- `SnapshotCadence`：每帧、每 2 帧、每 5 帧、每 10 帧。
- `SnapshotScope`：全局、按玩家兴趣、按区域、按队伍。
- `SnapshotPriority`：玩家、近处敌人、投射物、普通单位、远处单位。
- `SnapshotKind`：Full、Delta、KeyFrame、AuthorityOverride、Batch。

### 客户端消费层

客户端需要从一个 `ShooterClientFrameSyncController` 扩展为多种消费策略：

- 预测回滚控制器：现有能力继续保留。
- 插值控制器：维护 snapshot buffer，以服务器时间轴延迟播放远端实体。
- 批量同步控制器：按批次解码状态并生成插值段。
- 大规模 LOD 控制器：根据实体优先级和距离选择更新频率。
- 重连恢复控制器：处理 full snapshot 覆盖、订阅恢复和表现重建。

建议保留现有 `ShooterClientSession` 作为外层会话 facade，但内部不再固定只持有 frame sync coordinator，而是通过同步模式创建对应 controller。

### 时间轴与缓冲层

多种同步能力档案都依赖统一时间语义。现有 `WorldStartAnchor`、`ServerNowTicks`、snapshot `ServerTicks`、input response `ServerTicks` 应继续作为基础。

需要新增或沉淀：

- `ShooterRemoteSnapshotBuffer`：按服务器帧或 server ticks 存储远端快照。
- `ShooterInterpolationTimeline`：根据本地估算服务器时间和 interpolation delay 选择前后两个快照。
- `ShooterExtrapolationPolicy`：缺少新快照时允许短时间外推，超过阈值冻结或请求 resync。
- `ShooterClockSmoothing`：平滑服务端时间偏移，避免表现帧抖动。

### 恢复与重连层

当前漂移检测和强同步恢复不应丢弃，而应归入通用恢复层。不同同步模式可以复用同一组恢复原因：

- ImportFailed。
- AuthoritativeHashMismatch。
- ClientHashRejectedByServer。
- FrameTooFarBehind。
- FrameTooFarAhead。
- SnapshotTimeout。
- WorldMismatch。
- SnapshotGap。
- ReconnectRequested。

预测回滚模式收到这些原因时可能进入 `AwaitingFullSnapshot`。插值模式遇到 gap 时可能只请求 keyframe。大规模同步模式可能只请求当前 AOI 的 full slice。

## 协议演进方向

### 保持 Room Gateway 外壳通用

现有通用协议仍然应作为公共入口：

- Login。
- CreateRoom。
- JoinRoom。
- Ready。
- StartBattle。
- SubscribeStateSync。
- SubmitBattleInput。
- RequestFullStateSync。
- Snapshot push。

后续新增同步能力时，优先扩展 payload 和策略字段，而不是为每个玩法单独开 gateway 链路。

### 增加同步能力档案声明

第一阶段可以先在本地测试和 launcher 参数里声明兼容档案名。第二阶段建议扩展启动参数：

- Room tags 中携带 `syncProfile` 或兼容字段 `syncMode`。
- `WireStartRoomBattleReq` 或玩法 start payload 携带同步档案名。
- BattleInitParams 传递给 runtime adapter。
- Snapshot push 优先携带 `StreamId` / stream kind / capability metadata，便于客户端选择 decoder/controller；不建议长期只靠单个 `SyncMode` 判断所有消费逻辑。

### 增加快照流类型

当前 full/delta push 已经够支撑预测回滚。多模式后建议明确快照流：

- `AuthorityFull`：全量权威状态。
- `AuthorityDelta`：增量权威状态。
- `InterpolationState`：用于远端插值的实体状态。
- `BatchState`：多帧或多实体批量状态。
- `AoiSlice`：兴趣区域切片。
- `CriticalEvent`：关键事件流。
- `ResyncFull`：恢复专用 full snapshot。

具体可以先体现在 Shooter payload opCode 和 packed snapshot flags 中，待稳定后再提升到通用 Room 协议。

## 与现有文档的关系

### Shooter 示例落地设计

`Shooter示例落地设计.md` 仍作为第一阶段 Shooter 最小完整闭环与协议基线文档。里面关于 Room Gateway、WorldStartAnchor、输入链路、状态同步推送、晚进和重连的描述继续有效。

### Shooter 客户端漂移检测与强同步恢复方案

`Shooter客户端漂移检测与强同步恢复方案.md` 不再代表 Shooter 的全部同步方向，而是 `PredictRollback` 模式下的漂移检测和恢复子方案。里面的 state hash、强同步状态机、RequestFullStateSync 和 focused tests 仍然保留。

### Shooter 协议与战斗流程阶段性总结

`Shooter协议与战斗流程阶段性总结.md` 是当前重构前的冻结基线。后续新增同步模式时，应保护其中列出的通用边界，尤其是 Gateway 不解释玩法 payload、BattleId/WorldId 分工、WorldStartAnchor 时间域和 late join/reconnect 语义。

## 第一阶段推进计划

第一阶段目标不是一次性实现全部同步模型，而是把 Shooter 的结构从“固定帧同步 demo”打开成“可挂多种同步能力档案的 demo”。这里的阶段计划保留历史推进语境，具体 API 以后续能力矩阵口径为准。

### 阶段 1：文档和模式边界

- 新增本设计文档，明确 Shooter 新定位。
- 定义早期 `ShooterSyncMode` 枚举或等价配置作为 controller 分流入口；后续收敛为框架层能力档案。
- 标注现有客户端 frame sync 链路属于 `PredictRollback` 客户端播放/对账能力。
- 梳理现有 `ShooterClientSession`、`ShooterClientFrameSyncController`、`ShooterPackedSnapshotSyncController` 哪些是模式专属，哪些是通用能力。

验收：文档明确，代码中出现最小同步模式概念，但不要求行为变化。

### 阶段 2：客户端同步 controller 分层

- 保留现有预测回滚 controller。
- 引入 `IShooterClientSyncController` 或轻量 adapter，把 `StartGame`、`Tick`、`SubmitInput`、`ApplyGatewayPush`、`RequestResync` 等能力纳入统一 facade。
- `ShooterClientSession` 短期根据 `ShooterSyncMode` 或兼容档案名创建内部 controller。
- 当前默认档案仍为 `PredictRollback`，所有现有测试继续通过。

验收：现有功能不退化，模式切换入口存在。

### 阶段 3：权威插值模式原型

- 新增远端 snapshot buffer。
- 新增基于 server ticks 的 interpolation timeline。
- 客户端本地玩家仍可走输入预测，远端实体不回滚，只按延迟插值播放。
- 服务端先复用现有 packed snapshot 推送频率，不急于做 AOI。

验收：同一 Shooter 场景可选择 `AuthoritativeInterpolation`，客户端能消费权威快照并平滑表现远端实体。

### 阶段 4：低频批量状态同步

- 服务端支持每 N 帧发布一次 batch snapshot。
- batch payload 包含多个实体的状态和快照帧。
- 客户端按批次生成插值段。
- 增加测试覆盖 5 帧、10 帧下发时客户端表现帧连续。

验收：大量实体场景下不触发高频回滚，快照下发频率可配置。

### 阶段 5：大规模战场和兴趣管理演示

- 增加红蓝双方大量单位的模拟场景。
- 服务端按区域、距离或队伍筛选同步对象。
- 普通单位低频同步，近处敌人和本地相关对象高频同步。
- 关键事件单独广播。

验收：能展示大量单位下不同同步频率和兴趣管理策略的收益。

### 阶段 6：快速重连统一演示

- 客户端保存 session token、battle id、world id、最后应用快照帧和同步模式。
- 重连后 Join running battle，恢复订阅。
- 根据同步模式请求 full snapshot、keyframe 或 AOI slice。
- 表现层重建并恢复输入提交。

验收：PredictRollback、AuthoritativeInterpolation 至少两个模式都能走快速重连恢复。

## 测试与 Smoke 策略

### 单元测试

- 兼容档案名默认值和配置传递。
- `PredictRollback` 客户端能力保持现有行为。
- 插值 buffer 对乱序、缺帧、重复帧的处理。
- batch snapshot 解码和插值段生成。
- resync request 在不同模式下选择 full/keyframe/slice。

### 集成测试

- Gateway launcher 能按同步模式启动 session。
- Room flow 保持 BattleId、WorldId、WorldStartAnchor 不变。
- Snapshot push 可以被不同 controller 消费。
- Late join/reconnect 在不同同步模式下都能拿到有效初始状态。

### Smoke 测试

后续 smoke 不应只验证一种同步策略，而应至少覆盖：

- PredictRollback 基线。
- AuthoritativeInterpolation 基线。
- BatchStateSync 基线。
- Reconnect 恢复。
- ShouldResync 或 snapshot gap 恢复。

每条 smoke 输出应记录同步模式、快照频率、输入响应、客户端表现帧、服务端权威帧、恢复次数和最后状态摘要。

## 风险与约束

### 不要一次性重写现有链路

当前 Shooter 帧同步链路已经有大量单测和 smoke 保护，应作为 `PredictRollback` 模式保留。新模式应并行引入，逐步把通用部分抽出，避免为了抽象而打碎已稳定的协议边界。

### 避免 Gateway 变成玩法逻辑层

多同步模式会带来更多 payload 类型和策略字段，但 Gateway 仍应保持通用路由和诊断角色。具体同步语义由 Battle Host 和 Shooter adapter/controller 处理。

### 插值模式不能复用回滚假设

插值模式关注表现时间轴，不应该强行套用 pending input replay。它需要独立的 snapshot buffer、interpolation delay 和 extrapolation 策略。

### 大规模同步需要先做观测指标

万人红蓝大战不能只靠“能跑”判断方案好坏。必须先有实体数量、包大小、下发频率、客户端应用耗时、可见实体数量等指标，否则无法证明策略有效。

## 阶段性结论

Shooter 示例的新方向应是网络同步框架演示工程。现有双端帧同步不是废弃，而是作为 `PredictRollback` 客户端播放/对账能力保留下来，并继续承担“客户端预测、漂移检测、强同步恢复”的样板职责。

后续真正要扩展的是同步能力边界：让同一套 Room Gateway、Battle Host、WorldStartAnchor、Snapshot Push、Input Response 和 Presentation Projection 能服务于预测回滚、权威插值、低频批量同步、大规模兴趣管理、快速重连和服务器回溯判定这些可组合能力。这样 Shooter 才能成为 AbilityKit 网络同步能力的展示面，而不是被某一种同步范式或单一枚举限制住。

## Stage 3.5：AuthoritativeInterpolation 打磨项

Stage 3 落地了 `AuthoritativeInterpolation` 原型（`RemoteSnapshotBuffer` + `InterpolationTimeline` + 投影器），架构分层和单测护栏都已就位，但只是“正确的原型”，离生产级还差几项工程打磨。Stage 3.5 在不改变对外契约和分层的前提下，把以下问题补齐：

### 1. 旋转最短弧插值

新增框架层 `InterpolationMath`（`com.abilitykit.network.runtime`），集中提供 `Lerp` 与角度插值 `LerpAngle / LerpAngleRadians / LerpAngleDegrees`，避免各玩法层各自实现易错的角度插值。投影器对 `Rotation` 改用 `LerpAngleRadians`（Shooter 的 `Rotation` 由 aim 向量经 `Atan2` 语义得到，按弧度处理），沿最短弧混合，修正了跨 ±π 接缝时反向旋转一整圈的问题；位置、速度、血量等线性量仍走 `Lerp`。

### 2. 时间轴软收敛

`InterpolationTimeline` 由“硬 snap 到最新服务器时间”改为可配置的软追赶（catch-up）。内部用 `double` 维护 `_targetTicks` 与 `_estimatedTicks`：观测只前移 target，`Advance` 时按 `maxCatchUpRate` 施加被限幅的修正（`correction ≤ advance * rate`），从而平滑吸收本地/服务器时钟漂移，不会因为一帧的新观测直接跳变。为保持兼容，2 参构造函数等价于 snap 模式（rate=0），既有时间轴单测语义不变；控制器经 `ShooterClientInterpolationConfig.CatchUpRate`（默认 0.1）选用软模式。

### 3. 投影器零分配 + despawn 收尾

投影器由静态类改为有状态实例类 `ShooterRemoteSnapshotProjector`，复用内部 `List/Dictionary/HashSet`，稳态播放不再每帧 `new` 数组与字典。同时按 `from ∪ to` 并集投影：在 `to` 中消失（despawn）的实体会在中间帧保留上一姿态，避免插值途中突然消失的闪烁；下一段播放推进越过 `to` 后自然丢弃。投影产物为单消费者临时对象，表现管线同步消费并拷出字段，故复用安全。控制器改为持有投影器实例并调用实例方法。

### 4. 外推策略与饥饿标记

`ShooterClientInterpolationConfig` 新增 `MaxExtrapolationTicks`（默认 50ms）。当延迟播放时间跑到最新缓冲快照之后（缓冲饥饿）时，**不做姿态外推**（避免编造非权威运动），而是保持最新姿态；一旦超出容差，控制器通过新增的 `IsRemotePlaybackStarved` 标记暴露饥饿状态，供上层做连接质量提示等反应。

### 测试与现状

新增针对角度最短弧、软收敛（渐进/限幅）、despawn 收尾、饥饿标记的单测；既有两条控制器插值单测显式固定为 snap 模式（`catchUpRate: 0`）以保持播放时序确定性，软收敛由专门的时间轴单测覆盖。`AbilityKit.Demo.Shooter.Runtime.Tests` 全量 82 项通过。

打磨后 `AuthoritativeInterpolation` 已具备“最短弧插值、时钟软收敛、零分配播放、despawn 收尾、缓冲饥饿可观测”等生产级特性。仍待后续阶段处理的方向包括：`TrySample` 的 O(n) 线性扫描在大缓冲下可换为二分；以及结合后续观测指标对 `InterpolationDelay / CatchUpRate / MaxExtrapolation` 做按网络质量自适应的调参。

## Stage 3.6：插值能力接入真实 session 链路

Stage 3.5 把 `AuthoritativeInterpolation` 打磨到生产级，但能力只停在“控制器自己能用”这一层：自定义插值配置与饥饿/缓冲等可观测量仅被单测触达，真实的 `ShooterClientSession` → `ShooterClientSyncControllerFactory` 链路既无法注入配置，也读不到运行态指标。Stage 3.6 在不破坏既有契约的前提下把这两条缝补齐。

> 注：本节最初引入的是 Shooter 局部类型 `ShooterClientInterpolationConfig` / `ShooterInterpolationDiagnostics` / `IShooterInterpolationDiagnosticsProvider`。Stage 3.7 已将它们下沉为框架层通用类型 `InterpolationConfig` / `InterpolationDiagnostics` / `IInterpolationDiagnosticsProvider`，下文按下沉后的命名描述。

### 1. 配置贯通（向后兼容重载）

`ShooterClientSyncControllerFactory.Create` 与 `ShooterClientSession` 各新增一个可携带 `InterpolationConfig?` 的重载，旧签名委托到新重载并传 `null`；工厂在创建 `AuthoritativeInterpolation` 控制器时以 `interpolationConfig ?? InterpolationConfig.Default` 兜底。这样上层可以按需注入 `InterpolationDelay / CatchUpRate / MaxExtrapolation`，未传时行为与之前完全一致，既有调用点零改动。

### 2. 可观测量暴露（能力接口 + TryGet）

插值健康度（缓冲快照数、播放/估计服务器 tick、延迟 tick、是否已发布远端帧、是否缓冲饥饿）此前只挂在具体控制器上，对外不可见。Stage 3.6 不把这些塞进与同步模式无关的 `IShooterClientSyncController`，而是新增只读结构 `InterpolationDiagnostics` 与能力接口 `IInterpolationDiagnosticsProvider`，仅由真正做插值的控制器实现；`ShooterClientSession` 暴露 `TryGetInterpolationDiagnostics(out ...)`，内部按 `is` 类型判定返回。非插值模式（如 `PredictRollback`）调用时返回 `false` 且产出 `default`，调用方无需感知当前同步模式。

### 测试与现状

新增三项接入单测：自定义配置经 session 链路抵达控制器（以 snap 模式验证 `EstimatedServerTicks`）、插值模式经 session 取到正确诊断量（缓冲数/播放 tick/延迟 tick/已发布/未饥饿）、`PredictRollback` 模式经 session 取诊断返回 `false`。`AbilityKit.Demo.Shooter.Runtime.Tests` 全量 85 项通过。

接入后，`AuthoritativeInterpolation` 的配置与运行态指标已贯通到 session 层，可被冒烟输出、连接质量提示等上层消费。

## Stage 3.7：插值配置与诊断量下沉框架层

原则上 Shooter 只是网络同步框架的演示工程，不应沉淀本属于框架的能力。Stage 3.6 落地时把插值配置与诊断量先放在了 Shooter 包里，但它们都是与 Shooter 玩法零耦合的纯通用件：调参结构只描述 `TicksPerSecond / InterpolationDelay / BufferCapacity / CatchUpRate / MaxExtrapolation`，诊断量只描述缓冲数/播放 tick/饥饿标记。它们与框架层既有的 `RemoteSnapshotBuffer` + `InterpolationTimeline` + `InterpolationMath` 是同一族原语，理应同处一层，否则每个接权威插值的 demo 都得重抄一遍。

Stage 3.7 把三者下沉到 `com.abilitykit.network.runtime` 并去掉 `Shooter` 前缀：

- `ShooterClientInterpolationConfig` → `InterpolationConfig`（`AbilityKit.Network.Runtime`），构造重载、裁剪逻辑、`Default` 全部原样保留。
- `ShooterInterpolationDiagnostics` → `InterpolationDiagnostics`，`IShooterInterpolationDiagnosticsProvider` → `IInterpolationDiagnosticsProvider`。
- Shooter 侧 `ShooterClientAuthoritativeInterpolationSyncController` / `ShooterClientSyncControllerFactory` / `ShooterClientSession` 及相关单测改为直接消费框架类型，删除两个 Shooter 局部文件，不再保留别名。

下沉后 Shooter 在权威插值这条线上退回成“解码样本 + 投影到表现 + 选择同步能力档案”的薄演示，通用的 buffer/timeline/config/diagnostics 全部归框架层。全量回归 `AbilityKit.Demo.Shooter.Runtime.Tests` 85 项通过，行为不变。

仍留在 Shooter 的“通用编排”是 `ShooterClientAuthoritativeInterpolationSyncController` 里 buffer + timeline + 外推/饥饿策略的串联逻辑（每个 demo 仍需重写一遍）。若后续要让 Moba 等 demo 复用，可再抽框架层 `RemoteInterpolationPlayback<TSample>` 把这段编排也收上去，仅暴露“解码”“投影+应用”两个回调——这是下一档（B）的范围，已在 Stage 3.8 落地。至此“涉及接入”这一前置条件具备，可在此基础上再回头推进客户端表现层的抽象（见《客户端表现层框架契约设计》）。

## Stage 3.8：远端插值编排下沉框架层（RemoteInterpolationPlayback）

Stage 3.7 把配置与诊断量下沉后，唯一仍滞留在 Shooter 的通用件是 `ShooterClientAuthoritativeInterpolationSyncController` 里 buffer + timeline + 外推/饥饿策略的串联：每帧 `Advance` 时间线、按延迟播放时刻 `TrySample` 缓冲、依据 `ExtrapolationTicks` 与 `MaxExtrapolation` 判定饥饿、维护“是否已发布远端帧”等运行态标志。这段逻辑与 Shooter 玩法零耦合，任何接权威插值的 demo（如 Moba）都得重抄一遍。

Stage 3.8 在 `com.abilitykit.network.runtime` 新增泛型 `RemoteInterpolationPlayback<TSample>`，把这套编排连同其拥有的 `RemoteSnapshotBuffer<TSample>` + `InterpolationTimeline` + 饥饿策略一并收上去，对外暴露：

- `Observe(TSample)`：入缓冲并把样本的 `TimelineTicks` 折进时间线（陈旧/重复样本被拒，不推进时间线）。
- `Advance(deltaSeconds)`：推进延迟播放时间线。
- `TrySample(out RemoteSnapshotInterpolation<TSample>)`：在当前延迟播放时刻采样，更新 `IsStarved` / `HasPublished` 并返回插值结果。
- `GetDiagnostics()`：产出框架层 `InterpolationDiagnostics`。
- `BufferedSampleCount / PlaybackTicks / EstimatedServerTicks / IsStarved / HasPublished` 只读量与 `Reset()`。

下沉后 demo 只需提供两个回调式职责：把推送**解码**为 `TSample`（喂给 `Observe`），以及把采样结果**投影并应用**到表现层。`ShooterClientAuthoritativeInterpolationSyncController` 退薄为持有一个 `RemoteInterpolationPlayback<ShooterRemoteSnapshotSample>`，`Tick` 里 `_playback.Advance` + `PublishInterpolatedRemoteFrame`（仅 `TrySample` → `_projector.Project` → `_presentation.ApplyGatewaySnapshot`），`BufferRemoteSnapshot` 仅 `_playback.Observe`，所有诊断量与饥饿标志转为对 playback 的只读转发，控制器自身不再持有 buffer/timeline/外推阈值字段。控制器对外公开契约（属性、方法签名、`IInterpolationDiagnosticsProvider`）完全不变，既有接入与单测零改动。

新增 6 项框架级单测覆盖 `RemoteInterpolationPlayback`：未观测服务器时间前 `TrySample` 返回 `false`；观测+推进后在延迟播放时刻正确插值；陈旧样本被拒且不推进时间线；播放越过最新样本超容差时置位饥饿；诊断量如实反映播放态（含 `PlaybackDelayTicks`）；`Reset` 清空缓冲/时间线/标志。全量回归 `AbilityKit.Demo.Shooter.Runtime.Tests` 91 项通过（85 基线 + 6 新增），行为不变。

至此权威插值这条线的通用编排全部归框架层，Shooter 仅保留“解码 + 投影 + 选模式”的薄演示，Moba 等后续 demo 可直接复用 `RemoteInterpolationPlayback<TSample>`。
