# MOBA Flow Spec

> 状态：当前实现与目标态对照
> 目标：描述 MOBA 示例的流程规划，并明确哪些已经实现，哪些仍是正式项目目标。

---

## 1. 当前实现

当前 Unity MOBA view runtime 已实现的是一个最小可运行流程：

RootFlow:

- Boot
- Lobby
- Battle

BattleFlow:

- Prepare
- Connect
- CreateOrJoinWorld
- LoadAssets
- InMatch
- End

当前实现特点：

- Root/Battle 状态拓扑由 `MobaFlowConfiguration` 维护。
- 状态机执行由 HFSM 完成。
- transition 条件仍由 `GameFlowDomain` adapter 绑定，例如 battle requested。
- 状态进入后的 feature 装配由 `PhaseStateFeatureSpec`、`PhaseFeaturePlan`、`PhaseStateFeatureBinding` 和 `PhaseStateFeatureRegistry` 完成。
- feature 生命周期由 `PhaseFeatureHost` 管理。

当前 Root 转移：

| Trigger | From | To | Condition |
|---|---|---|---|
| BootCompleted | Boot | Lobby | none |
| EnterBattle | Lobby | Battle | battle_entry_ready |
| EnterBattle | Boot | Battle | battle_entry_ready |
| ReturnLobby | Battle | Lobby | none |
| ReturnLobby | Boot | Lobby | none |

当前 Battle 转移：

| Trigger | From | To |
|---|---|---|
| PrepareDone | Prepare | Connect |
| Connected | Connect | CreateOrJoinWorld |
| JoinedWorld | CreateOrJoinWorld | LoadAssets |
| LoadingDone | LoadAssets | InMatch |
| Ended | InMatch | End |

当前 feature 组合：

| State | Features | Clear Before Enter |
|---|---|---|
| Boot | none | yes |
| Lobby | none | yes |
| Battle.Prepare | context, entity, session | yes |
| Battle.Connect | debug_ongui | no |
| Battle.CreateOrJoinWorld | debug_ongui | no |
| Battle.LoadAssets | debug_ongui | no |
| Battle.InMatch | sync, input, view, hud, debug_ongui | no |
| Battle.End | debug_ongui | yes |

---

## 2. 当前实现仍不够正式的地方

### 2.1 flow state/event 已独立，但仍是最小集合

当前 Root/Battle 的 state/event 已抽到 `MobaFlowTypes`，`MobaFlowConfiguration` 和 `GameFlowDomain` 都依赖同一份 flow definition，不再由配置反向依赖 Domain 的嵌套枚举。

不过这仍只是当前最小可运行集合。正式项目目标态里的 Auth、Matchmaking、Room、PostBattle、Connectivity 等状态域尚未落地。

### 2.2 condition resolver 已独立，正式流程条件上下文已预留
 
`battle_requested`、`authenticated`、`room_ready`、`connectivity_ready`、`assets_ready` 和组合条件 `battle_entry_ready` 已经由 `MobaFlowConditionIds` 维护，并通过 `MobaFlowConditionResolver` 从 condition id 解析到项目侧判断逻辑。`GameFlowDomain` 只负责把当前运行态组装成 `MobaFlowConditionContext`，再交给 resolver。
 
当前 Root 进入 Battle 的配置已改为 `battle_entry_ready`。由于 Auth、Room、Connectivity、AssetLoading 等正式系统还没有在示例流程里展开，`GameFlowDomain` 现在先用默认 ready 状态保持最小示例可运行；后续接入真实系统时，只需要替换 `BuildFlowConditionContext` 的上下文来源。

### 2.3 enter/exit action refs 已进入通用 spec 和生命周期
 
当前 `_battleSessionStarted`、`_battleFirstFrameReceived` 重置，以及 Battle.End 后回 Lobby，已经抽到 `MobaFlowActionIds` 和 `MobaFlowActionExecutor`。`PhaseStateFeatureSpec` 可以声明 enter before、enter after、exit action refs；`PhaseStateFeatureBindingFactory` 会把 enter before/after refs 接到 enter 生命周期，把 exit refs 接到 exit 生命周期，再由 `GameFlowDomain` 的项目侧 executor 执行。
 
当前已经落地：
 
- Battle.Prepare 通过 `MobaFlowConfiguration` 声明 enter before action ref。
- Battle.End 通过 `MobaFlowConfiguration` 声明 enter after action ref。
- `PhaseStateFeatureBinding` 提供 exit 生命周期。
- `PhaseStateFeatureRegistry` 提供按 state key 退出的 registry 入口。
- MOBA 的 HFSM state onExit 已接到对应 registry exit，未来配置里增加 exit action refs 不需要再改状态机回调代码。
 
当前校验能力：
 
- `PhaseActionCatalog` 可登记合法 action id。
- `PhaseStateFeatureValidator` 可校验 enter before、enter after、exit action refs 的重复和未知 id。
 
仍待继续：
 
- switch flow refs 尚未建模。

### 2.4 当前只实现了最小 RootFlow

Auth、Matchmaking、Room、PostBattle、Connectivity 并行域仍是目标态，不是当前实现。

---

## 3. 正式项目目标态

目标 RootFlow:

- Boot
- Auth
- Lobby
- Matchmaking
- Room
- Battle
- PostBattle

目标 Root 事件：

- Sys.Started
- Auth.LoginRequested
- Auth.LoginSucceeded
- Auth.LoginFailed
- Auth.LogoutRequested
- Match.StartQueue
- Match.CancelQueue
- Match.Found
- Room.Joined
- Room.ReadyChanged
- Room.BpStarted
- Room.BpFinished
- Battle.EnterRequested
- Battle.Ended
- Conn.Disconnected
- Conn.ReconnectSucceeded
- Conn.Kicked

目标 Root 转移表：

| From | Event | To |
|---|---|---|
| Boot | Sys.Started | Auth |
| Auth | Auth.LoginSucceeded | Lobby |
| Lobby | Match.StartQueue | Matchmaking |
| Matchmaking | Match.Found | Room |
| Room | Room.BpFinished | Battle |
| Battle | Battle.Ended | PostBattle |
| PostBattle | ReturnLobby | Lobby |
| Any | Auth.LogoutRequested | Auth |
| Any | Conn.Kicked | Auth |

---

## 4. RoomFlow 目标态

RoomFlowState:

- Assemble
- Ready
- BP
- Confirm
- ExitToBattle

目标转移：

| From | Event | To |
|---|---|---|
| Assemble | Room.Joined | Ready |
| Ready | Room.BpStarted | BP |
| BP | Room.BpFinished | Confirm |
| Confirm | Battle.EnterRequested | ExitToBattle |

RoomFlow 应产出 BattlePlan，供 BattleFlow 使用。

---

## 5. BattleFlow 目标态

BattleFlowState:

- Prepare
- LoadAssets
- Connect
- CreateOrJoinWorld
- InMatch
- End
- Return

目标转移：

| From | Event | To |
|---|---|---|
| Prepare | PrepareDone | LoadAssets |
| LoadAssets | Battle.LoadingDone | Connect |
| Connect | Battle.Connected | CreateOrJoinWorld |
| CreateOrJoinWorld | Battle.JoinedWorld | InMatch |
| InMatch | Battle.Ended | End |
| End | ReturnRequested | Return |

注意：当前实现中 Connect 在 LoadAssets 前，目标态可能更适合先准备资源再连接，或根据真实项目需求保留当前顺序。这个需要结合资源加载、网关连接、首帧等待策略再定。

---

## 6. Connectivity 并行域目标态

ConnectivityState:

- Online
- Reconnecting
- Kicked
- Offline

规则：

- Conn.Kicked 必须打断任何主流程，清理会话并回到 Auth。
- Conn.Disconnected 可进入 Reconnecting，并暂停或降级当前 feature。
- Conn.ReconnectSucceeded 根据上下文恢复 Room 或 Battle。

当前框架尚未直接建模并行域。建议仍由 HFSM 或外层协调器处理，Client Flow 只负责 feature 清理、action/flow 执行和诊断。

---

## 7. 下一步落地顺序
 
1. 扩展 `MobaFlowConfiguration`，让配置更接近正式 RootFlow。
2. 继续梳理 switch flow refs，让状态切换时的异步 flow/work 节点也能配置化。
3. 将 Auth、Room、Matchmaking、Connectivity 逐步接入 `BuildFlowConditionContext` 的真实运行态来源。
4. 最后再处理 Auth、Room、Matchmaking、Connectivity 这些完整目标态。
