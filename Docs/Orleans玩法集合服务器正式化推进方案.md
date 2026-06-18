# Orleans 服务器玩法集合正式化推进方案

## 当前定位

`AbilityKit.Orleans.Grains` 已经不只是单一战斗示例，而是在承担玩法集合服务器的核心编排职责：账号会话、房间目录、房间成员生命周期、玩法准备流程、战斗运行时选择、输入提交、状态同步推送。

当前已有 MOBA 与 Shooter 两类示例玩法，因此服务器层需要从“示例拼装”推进为“可承载多玩法的正式宿主”。

## 现状分层

- 账号层：`Accounts/SessionGrain.cs` 提供 Guest 登录、Session 校验、续期、登出，暂时独立于玩法。
- 房间层：`Rooms/RoomDirectoryGrain.cs` 管理区服目录和公开房间列表，`Rooms/RoomGrain.cs` 管理成员、离线、恢复、准备、选择、启动战斗。
- 房间玩法层：`Rooms/Gameplay/IRoomGameplayAdapter.cs` 已抽象不同玩法的房间状态、准备规则和战斗初始化参数构建。
- 战斗层：`Battle/BattleLogicHostGrain.cs` 负责通用战斗生命周期、输入缓冲、Tick 和状态同步，具体运行时由 `IBattleRuntimeAdapter` 提供。
- 网关层：`AbilityKit.Orleans.Gateway` 负责 wire 协议、Session 校验、请求路由，并直接调用 Orleans Grains。

## 主要问题

- 玩法类型 ID 分散：默认 `battle` 同时出现在网关、房间适配器、战斗日志、MOBA 协议映射中，容易产生不一致。
- 注册机制偏示例：房间与战斗运行时注册由 `new` 写死在 Grain 或 Registry 内部，后续新增玩法需要修改核心宿主代码。
- 通用房间协议带玩法字段：`RoomPickHeroRequest` 与 `RoomPlayerSnapshot` 暂时明显偏 MOBA，Shooter 只能忽略或填默认值。
- 房间状态未持久化：RoomGrain/DirectoryGrain 当前主要是内存状态，适合 demo，但不适合正式房间生命周期。
- 错误语义不够正式：大量 `InvalidOperationException` 作为业务错误，网关多处折叠成 InternalError，客户端难以稳定处理。
- 生命周期边界偏粗：房间关闭、战斗销毁、成员离线超时、重连恢复、观战/中途加入还没有统一策略对象。

## 推进路线

### 阶段 1：统一玩法目录与默认类型

- 将通用玩法类型常量放到 Contracts，网关、房间、战斗统一引用。
- 引入玩法描述对象，至少包含 `RoomType`、显示名、默认 MaxPlayers、是否需要选角、默认同步模板。
- 让房间和战斗 Registry 从同一玩法目录派生，避免各自维护默认值。

### 阶段 2：拆分通用房间动作与玩法动作

- 保留 `Ready`、`Join`、`Leave`、`StartBattle` 作为通用动作。
- 将 `PickHero` 收敛为玩法负载动作，例如 `SubmitRoomGameplayCommand` 或 `ConfigurePlayerLoadout`。
- RoomSnapshot 增加通用 PlayerState + 可选玩法扩展 payload，避免每个玩法被 MOBA 字段污染。

### 阶段 3：正式化业务错误与网关响应

- 定义房间错误码、战斗错误码、Session 错误码。
- Grain 返回 Result 模型，网关按错误码映射 BadRequest/Forbidden/Conflict/NotFound/InternalError。
- 客户端协议保留 message，但以稳定 code 为主要判断依据。

### 阶段 4：生命周期和持久化

- RoomDirectory 与 RoomGrain 引入 Orleans 状态持久化或明确的外部存储接口。
- 定义房间状态机：Lobby、Starting、InBattle、Closing、Closed、Expired。
- 将离线超时、房主转移、空房回收、战斗结束回写做成策略。

### 阶段 5：玩法插件化

- 将 MOBA/Shooter 注册迁移到 DI 或模块发现机制。
- BattleRuntimeAdapter 与 RoomGameplayAdapter 使用同一个玩法 manifest 校验成对注册。
- 允许 server host 通过配置启用/禁用玩法。

## 首批已落地方向

本轮先做低风险基础收敛：把默认房间类型提升到 `AbilityKit.Orleans.Contracts.Rooms.GameplayRoomTypes`，减少网关、房间和战斗之间的硬编码分散，为后续玩法目录/manifest 做准备。

随后继续推进房间协议边界收敛：新增 `RoomGameplayCommandRequest` 作为通用房间玩法命令载体，并拆分到独立的 `RoomGameplayCommandModels.cs`。旧的 Grain 级 `PickHeroAsync` 入口已移除，`RoomGrain` 只暴露 `SubmitGameplayCommandAsync` 下发玩法命令。外部 wire/HTTP 的 PickHero 请求暂时保留为兼容入口，但转换边界收敛到网关 mapper，由 `RoomGameplayCommandRequest.CreateMobaLoadout` 构造正式命令后再进入房间 Grain。MOBA 适配器负责解析自己的 loadout 命令，Shooter 适配器则显式忽略房间玩法命令。

本轮开始推进网关错误语义正式化：先在 HTTP API 层新增房间错误映射，将 Grain 暂时抛出的 `ArgumentException` / `InvalidOperationException` 转换为稳定的 HTTP status 与业务 code，覆盖创建房间、加入房间、准备、PickHero 兼容入口和开始战斗。该做法不立即改变 Orleans Grain 契约，后续可继续把同一套 code 下沉为正式的 Grain Result 模型。

随后补齐 TCP 网关侧的房间错误映射：新增 `RoomGatewayErrorMapper`，并在创建房间、加入房间、恢复房间、准备、PickHero 兼容入口和开始战斗 handlers 中复用，将房间满员/关闭映射为 `409 Conflict`，成员权限错误映射为 `403 Forbidden`，玩法命令错误映射为 `400 BadRequest`，避免所有业务失败都折叠为 `500 InternalError`。

本轮开始补自动化成员基础能力，但避免让机器人逻辑侵入房间核心：`RoomMemberState` 增加 `IsBot` 作为成员 metadata，房间契约新增 `JoinRoomMemberRequest`，`IRoomGrain.JoinMemberAsync` 只表达“接纳一个成员进入房间”。真人登录链路继续走现有 `JoinAsync`，后续专门的机器人模块可自行创建 bot 账号、调度行为并调用通用成员加入入口；房间 Grain 不负责批量生成 bot、不提供 bot 专用 HTTP API，也不把机器人和真人放到不同生命周期层级。

本轮补齐 HTTP/Web 调试闭环：网关新增玩法列表、公开房间列表、房间快照查询入口，并让 HTTP 创建/加入房间后同步绑定账号房间映射，和 TCP 网关恢复链路保持一致。`wwwroot/debug/index.html` 从最小请求页扩展为网页控制台，覆盖游客登录、玩法选择、创建房间、查询/加入房间、准备、MOBA loadout 配置、启动战斗与运行态查看，便于直接在浏览器中把玩法服务器跑到 Battle 启动阶段。

继续按正式项目流程补齐 HTTP 标准入口：新增账号登录、Session 校验、续期、登出、恢复当前房间和离开房间 API。账号登录复用 `CreateSessionForAccountAsync`，为后续真实账号系统接入预留边界；登出会先根据账号房间映射把当前房间成员标记离线，保留重连恢复语义；恢复当前房间复用 TCP 网关已有的 account-room mapping 与 `IRoomGrain.RestoreAsync` 逻辑，使 HTTP/Web、TCP 客户端在登录、重连、房间恢复上的行为逐步对齐。

本轮整理 `AbilityKit.Orleans.Grains` 工程目录：共享宿主能力继续保留在 `Accounts`、`Rooms`、`Battle`、`FrameSync` 与 `Gameplay` 下，其中 `Rooms/Gameplay` 和 `Battle/Gameplay` 只保存通用 adapter/registry 抽象；MOBA 与 Shooter 示例实现迁入 `Gameplays/Moba`、`Gameplays/Shooter`，并分别按 `Rooms`、`Battle`、`Protocol` 子目录管理。该调整不改变运行行为，先建立正式项目中“通用宿主层”和“玩法模块层”的物理边界，后续可继续把当前注册表中的手动 `new` 演进为 DI/manifest 驱动的玩法插件注册。

本轮开始为后续帧同步战斗服 UDP 接入整理 Gateway 边界：新增传输无关的 `IGatewayTransportEvents`，并扩展 `IGatewayTransportSession`，让业务路由、请求响应、服务器推送和会话注册只依赖通用传输会话，不再直接依赖 `TcpTransportSession`。现有 TCP 服务作为一个 transport adapter 实现该接口，后续 UDP/WebSocket 接入应复用同一事件模型与 handler/router，不把 UDP 逻辑硬编码进 Battle Grain 或业务 handler；UDP 侧只需要负责连接标识、端点重绑定、心跳和包投递适配。

本轮补齐 Shooter 大房间与服务器自动化验证入口：房间热加入不再只停留在成员列表，而是通过 `IRoomGameplayAdapter.BuildLateJoinPlayer`、`IBattleLogicHostGrain.JoinPlayerAsync` 和 `IBattleRuntimeSession.JoinPlayer` 增量写入正在运行的 Shooter runtime，并立即推送完整快照。新增 `IShooterSandboxGrain` 与 `ShooterSandboxGrain` 作为独立 automation 模块，按正式房间流程创建公开 Shooter 房间、让机器人以普通 `RoomMemberState.IsBot` 成员加入/准备/开战；战斗启动后通过 `IBattleLogicHostGrain.MountBotAiAsync` 为 bot playerId 挂载 `simple-battle` AI，AI 实现在 Shooter 逻辑层，可直接读取 `ShooterBattleState` 与 `IShooterEntityManager` 中的当前游戏状态来寻找目标、判断距离和生成战斗指令。AI profile 由 JSON 配置解析为运行时 HFSM，基于 HFSM Core Extension 的 `CompositeActionState` 与 `IActionBehaviour` 组织 `Wander` / `Chase` / `Attack` 行为，并在 simulation tick 前写入 `LatestCommands`，继续复用同一条战斗输入消费路径。Gateway 新增 `/api/shooter-sandbox/start`、`/api/shooter-sandbox/{sandboxId}`、`/api/shooter-sandbox/stop`，Web Debug Console 增加 Shooter Sandbox 控制卡片；真实客户端仍通过标准登录、加入房间、订阅状态同步进入该运行中战斗。

AI 后续按优先级分两层推进：第一优先级是玩家级 bot AI，继续把 bot 视为普通 player actor，只在逻辑层挂载 AI profile，并通过 runtime 冒烟测试验证“可移动、可攻击、可复用玩家输入消费路径”；第二优先级是战斗内敌人/怪物 AI，应先补独立的 Monster/NPC actor 数据与阵营/仇恨/刷怪规则，再挂载怪物 AI profile，不应复用房间成员或玩家账号语义。性能路径上，当前 JSON/HFSM controller 适合小规模验证；当 AI 数量上升到大量 bot/monster 时，应迁移为 Svelto ECS 形式：用 AI intent/blackboard/target/cooldown 组件存储状态，用 engines 批量查询实体和写入 command/intent，减少 per-agent 对象与虚调用开销，同时保留 JSON profile 作为 authoring 输入并在加载期编译成 ECS 可执行数据。
