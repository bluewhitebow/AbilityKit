# Shooter 客户端漂移检测与强同步恢复方案

## 背景

Shooter 当前是客户端和服务器共同推进帧同步：客户端本地运行预测，服务器运行权威世界，并通过 Gateway 推送权威快照。客户端运行时仍使用浮点计算，未全面切到定点数，因此即使输入帧一致，也可能因为浮点误差、平台差异、模拟顺序或本地临时状态差异，出现客户端状态和服务器状态逐帧漂移。

现有代码已经具备几个可复用基础能力：

- `ShooterBattleRuntimePort.ComputeStateHash()` 会按固定顺序把 `CurrentFrame`、玩家位置、朝向、血量、分数、存活状态、子弹位置、速度、剩余帧等关键状态量量化后计算哈希。
- `ShooterBattleRuntimePort.ExportPackedSnapshot()` 会把服务器权威帧、`StateHash` 和 packed component chunks 一起写入快照。
- `ShooterBattleRuntimePort.ImportPackedSnapshot()` 当前是整体覆盖语义，会重置本地状态并导入 packed 快照。
- `ShooterClientFrameSyncController.ApplyGatewayPush()` 收到 packed 快照后，会导入权威状态，再通过 `ClientPredictionReconciliationCoordinator` 裁剪已确认输入并 replay 剩余 pending input。
- `ShooterClientFrameSyncController.CatchUpToFrame()` 已经支持本地按固定步长追帧。
- `ShooterClientFrameSyncController.TryRestorePredictedSnapshot()` 已经支持从 rollback buffer 恢复本地预测帧。
- `NeedsFullSnapshotResync` 和 `LastResyncReason` 已经能标记客户端需要全量快照恢复。
- 服务端 `RequestFullSnapshotAsync()` 已支持定向向某个 observer 推全量快照；当前 Gateway 侧重复调用 `SubscribeStateSync` 时，同一个 observer 会触发 `RefreshFullSnapshot`，从而复用这条全量快照路径。

因此后续不需要另起一套同步系统，应该在现有权威快照、预测回放、全量覆盖能力上加一层明确的“漂移检测与恢复决策”。

## 目标

1. 每帧或按策略采样客户端关键状态哈希，和服务器权威哈希做可解释的比较。
2. 发现异常后不要继续静默预测，进入强同步恢复流程。
3. 区分普通权威校正、短距离追帧恢复、长距离全量覆盖恢复。
4. 让异常原因、权威帧、本地帧、哈希值可以被日志、测试和上层 UI 观察到。
5. 第一阶段尽量复用已有协议与代码；第二阶段再补更清晰的专用 resync opcode 和原因枚举。

## 当前能力边界

### 状态哈希

当前 `ComputeStateHash()` 已经覆盖战斗关键状态，并对浮点值做量化。这适合作为第一阶段漂移检测依据，但要注意两点：

- 哈希适合判断“是否一致”，不适合直接定位“哪里不一致”。后续调试可以追加 debug-only 分项哈希，例如 player hash、projectile hash。
- 当前哈希包含 `CurrentFrame`，因此比较必须发生在相同权威帧语义下。不能拿客户端预测到更远帧后的 hash 直接和旧权威帧 hash 比。

### 权威快照应用

当前 packed 快照导入是全量覆盖语义。即使 `SnapshotFlags` 标记为 delta，`ImportPackedSnapshot()` 仍然会重置本地状态再导入 chunk。因此：

- 作为“异常强同步”使用是安全的，因为它本来就是覆盖。
- 作为真正 delta merge 还不完整，需要后续单独实现 delta import 语义。
- 展示层已区分 full/delta，避免 packed delta 被错误当成 full 去删除缺失实体；但运行时导入仍不是 delta merge。

### 预测回放

`ClientPredictionReconciliationCoordinator` 当前流程是：

1. 记录导入前本地预测目标帧 `replayTargetFrame`。
2. 导入服务器权威快照到 `authoritativeFrame`。
3. 裁剪 `confirmedFrame` 之前的本地 pending input。
4. replay 剩余 pending input，直到追到导入前预测目标帧。
5. 比较 `authoritativeStateHash` 和 `importedStateHash`，判断快照导入后本地是否等于服务器权威状态。

这里已经有“覆盖后追帧”的核心动作，但还缺少恢复阈值和异常状态机。

### 服务端全量快照入口

当前短期可复用路径：客户端再次发送 `SubscribeStateSync`，服务端 `StateSyncObserverGrain` 判断同一 battle 重复订阅后触发 `RequestFullSnapshotAsync()`，向该 observer 定向推送 full snapshot。

这个路径能用，但语义偏绕。长期建议新增显式请求，例如 `RequestFullStateSync` 或 `RequestBattleResync`，请求体带 `BattleId`、`WorldId`、`ClientFrame`、`LastAuthoritativeFrame`、`ClientStateHash`、`Reason`，响应仅表示请求是否受理，真正恢复仍由后续 full snapshot push 完成。

## 漂移检测策略

### 比较点

推荐第一阶段只在“收到服务器权威 packed 快照并导入后”比较：

- 快照内 `StateHash` 是服务器在 `snapshot.Frame` 的权威哈希。
- 客户端导入该快照后立即调用 `ComputeStateHash()`，得到 `importedStateHash`。
- 两者不一致说明：协议解码、导入逻辑、快照内容或哈希算法存在异常，属于强同步级别问题。

第二阶段再增加“客户端提交输入时携带本地哈希”的检测：

- 客户端发送 input 时带 `ClientFrame` 和 `ClientStateHash`。
- 服务端在相同帧已有权威 hash 或历史 hash 时比较。
- 服务端发现异常后在 input response 中返回更明确的 `ShouldResync`、`ResyncReason`、`AuthoritativeFrame`、`AuthoritativeHash`。

这样能更早发现客户端漂移，而不是等下一个快照。

### 哈希历史

为了让服务端能比较客户端旧帧 hash，需要保留一个短窗口的权威帧 hash ring buffer，例如 120 到 240 帧：

- key: frame
- value: stateHash
- 可选 value: serverTicks、entityCount、snapshotFlags

客户端也可以保留短窗口本地 hash，用于日志和测试，但恢复决策以服务器权威为准。

### 异常分类

建议统一为以下原因枚举，先在客户端内部使用，协议化时再落到 wire DTO：

- `ImportFailed`：packed 快照无法导入。
- `AuthoritativeHashMismatch`：导入权威快照后，本地 hash 不等于快照 hash。
- `ClientHashRejectedByServer`：服务端比较客户端上报 hash 后判定不一致。
- `FrameTooFarBehind`：客户端落后服务器太多。
- `FrameTooFarAhead`：客户端预测领先服务器太多。
- `SnapshotTimeout`：长时间未收到权威快照。
- `WorldMismatch`：`WorldId` 不一致。

## 恢复策略

### 普通权威校正

适用条件：

- 收到合法 packed snapshot。
- 导入后 `importedStateHash == authoritativeStateHash`。
- 客户端预测帧和权威帧差值在可接受范围内。

处理方式：

1. 导入权威 snapshot。
2. 裁剪已确认 input。
3. replay 未确认本地 input。
4. 发布 reconciliation 结果。
5. 清除 `NeedsFullSnapshotResync`。

这是当前已有流程，应继续作为默认路径。

### 小差值追帧恢复

适用条件：

- 客户端落后服务器权威帧不多，例如 `0 < authoritativeFrame - localFrame <= SmallCatchUpThreshold`。
- 当前没有 hash mismatch 或 import failure。
- 客户端仍处于同一 `WorldId`。

处理方式：

1. 暂停接受新的本地预测提交，或将输入先缓存。
2. 调用 `CatchUpToFrame(authoritativeFrame)` 或按服务器目标帧追帧。
3. 追帧后再等待/应用下一次权威快照校验 hash。
4. 如果追帧过程中失败或超过最大追帧预算，升级为全量覆盖。

推荐阈值：

- `SmallCatchUpThreshold`: 6 到 10 帧。
- `MaxCatchUpTicksPerUpdate`: 2 到 4 帧，避免单帧卡顿。
- 超过阈值则不要硬追，直接请求全量快照。

### 中等差值覆盖后 replay

适用条件：

- 收到 full snapshot 或 authority override snapshot。
- 本地预测领先权威帧，但差距仍在 rollback/pending input 可回放范围内，例如 `0 <= localFrame - authoritativeFrame <= ReplayThreshold`。
- 快照导入后 hash 匹配。

处理方式：

1. 导入权威 snapshot，覆盖到 `authoritativeFrame`。
2. 裁剪已确认 input。
3. replay 未确认 input 到原本预测帧或当前时间推导目标帧。
4. 如果 replay 后继续收到 hash mismatch，标记强同步。

这就是当前 `ApplyGatewayPush()` 接近在做的事情，后续需要把阈值和结果分类显式化。

推荐阈值：

- `ReplayThreshold`: 不超过 rollback buffer 的一半，例如 buffer 240 时取 60 到 120。
- replay 超过阈值或 pending input 不完整时，直接全量覆盖并清空本地 pending input。

### 大差值全量覆盖

适用条件：

- import failed。
- authority hash mismatch。
- 本地帧和权威帧差值超过阈值。
- 服务端 input response 返回 `ShouldResync`。
- 快照长时间超时或客户端 world identity 不匹配。

处理方式：

1. 标记 `NeedsFullSnapshotResync = true`，记录原因、客户端帧、服务器帧、哈希值。
2. 停止继续做普通预测回放；可以继续收集用户输入，但不要提交为已确认预测状态。
3. 请求服务器定向 full snapshot。
4. 收到 full snapshot 后导入覆盖。
5. 导入后校验 `StateHash`。
6. 成功后清空 resync 状态，重置 accumulator，重新捕获 rollback snapshot。
7. 根据策略决定是否 replay 最近输入：
   - 如果 full snapshot 帧到本地目标帧差距很小，且 pending input 完整，可以 replay。
   - 如果差距大或异常原因是 hash mismatch/import failed，优先清空 pending input，从权威状态重新开始。

第一阶段可复用重复 `SubscribeStateSync` 请求触发 full snapshot。第二阶段应补专用 resync 请求，避免把“订阅”和“恢复”语义混在一起。

## 推荐状态机

客户端 frame sync 增加一个轻量状态机：

- `Normal`：正常预测、提交输入、应用权威快照。
- `CatchUp`：客户端落后较少，限制每 update 追帧数，追到目标后回到 `Normal`。
- `AwaitingFullSnapshot`：已发现异常或差值过大，等待 full snapshot。
- `ApplyingFullSnapshot`：正在导入 full snapshot 并校验 hash。
- `Recovered`：恢复成功后的短暂观测状态，可立即转回 `Normal`。

关键原则：

- `AuthoritativeHashMismatch` 不应该继续普通 replay；它说明导入后的权威基线都不可信，必须请求 full snapshot 或断线重连式恢复。
- `ImportFailed` 也必须进入 `AwaitingFullSnapshot`。
- 普通 delta snapshot 不应该用于强恢复，强恢复必须等 full snapshot 或 authority override snapshot。

## 协议演进建议

### 第一阶段：复用现有协议

无需改 wire DTO：

- 客户端发现 `NeedsFullSnapshotResync` 后，调用已有 `SubscribeStateSyncAsync()`。
- 服务端同一 observer 重复订阅触发 `RefreshFullSnapshot`。
- 服务端通过 `SnapshotPushed` 推 full snapshot。
- 客户端导入 full snapshot，校验 hash，清除 resync 状态。

优点：改动小，能先打通闭环。

缺点：语义不直观，日志和测试要明确说明这是临时复用路径。

### 第二阶段：新增专用 resync 请求

新增 opcode：

- `RequestBattleResync` 或 `RequestFullStateSync`。

请求字段建议：

- `SessionToken`
- `BattleId`
- `WorldId`
- `ClientFrame`
- `LastAuthoritativeFrame`
- `ClientStateHash`
- `Reason`

响应字段建议：

- `Success`
- `Accepted`
- `Message`
- `CurrentFrame`
- `ServerTicks`

服务端收到后：

1. 校验账号、battle、world。
2. 定位该账号对应 observer。
3. 调用 `RequestFullSnapshotAsync(observer)`。
4. 后续仍通过 `SnapshotPushed` 推 full snapshot。

### 第三阶段：输入响应携带更完整 resync 信息

扩展 `WireSubmitBattleInputRes`：

- `ResyncReason`
- `AuthoritativeFrame`
- `AuthoritativeHash`
- `FrameDelta`

这样客户端收到 input response 时就能决定：小差值追帧，还是直接请求全量覆盖。

## 最小落地顺序

1. 客户端内部增加 `ShooterClientDriftRecoveryPolicy`，集中配置阈值：`SmallCatchUpThreshold`、`ReplayThreshold`、`MaxCatchUpTicksPerUpdate`、`SnapshotTimeoutTicks`。
2. 在 `ShooterClientFrameSyncController` 内把当前 hash mismatch/import failed 从单纯标记，升级为明确的 recovery state。
3. 在 session/coordinator 层增加 `RequestFullSnapshotResyncAsync()`，第一阶段内部复用 `SubscribeStateSyncAsync()`。
4. 收到 full snapshot 时，只有 hash 校验成功才清除 resync 状态。
5. 给 input response 的 `ShouldResync` 增加客户端处理：收到后进入 `AwaitingFullSnapshot` 并请求 full snapshot。
6. 增加 focused unit tests：import failed、hash mismatch、small catch-up、large full overwrite、duplicate subscribe refresh full snapshot。
7. 再考虑新增专用 opcode 和 wire DTO。

## 测试计划

### 单元测试

- packed full snapshot 导入后 hash 匹配，`NeedsFullSnapshotResync` 清除。
- packed snapshot 导入失败，进入 `AwaitingFullSnapshot`。
- packed snapshot 携带错误 hash，进入 `AwaitingFullSnapshot`。
- 本地落后小于阈值，调用追帧策略，不请求 full snapshot。
- 本地落后大于阈值，请求 full snapshot。
- full snapshot 恢复成功后清空 recovery state，并捕获新的 rollback snapshot。
- input response `ShouldResync = true` 时进入 full snapshot 恢复。

### Orleans/Gateway 测试

- 同一 account、room、battle 重复 `SubscribeStateSync` 会触发 `RequestFullSnapshotAsync()`。
- full snapshot push 的 opCode 是 `SnapshotPushed`，delta snapshot push 的 opCode 是 `DeltaSnapshotPushed`。
- 请求 full snapshot 后，客户端最终收到包含 packed payload 和 state hash 的 full snapshot。

### Smoke 测试

- 正常流程：不触发 resync，hash 匹配。
- 人为篡改客户端 hash 或快照 hash：客户端进入 resync，重新请求 full snapshot，恢复后继续提交输入。
- 客户端暂停若干帧后恢复：小差值追帧，大差值 full overwrite。

## 阶段性结论

当前最合理的方向是：保留“客户端预测 + 服务器权威快照 + 导入后 replay”的主流程，把 per-frame hash 作为漂移检测信号，把异常恢复显式建模为独立状态机。

短期不需要重写帧同步。先复用已有 `StateHash`、`ImportPackedSnapshot()`、`CatchUpToFrame()`、`NeedsFullSnapshotResync` 和重复订阅触发 full snapshot 的服务端能力，就可以落地第一版断线式强同步恢复。后续再把“请求全量同步”从重复订阅中拆成专用 opcode，并补齐服务端对客户端上报 hash 的主动校验。
