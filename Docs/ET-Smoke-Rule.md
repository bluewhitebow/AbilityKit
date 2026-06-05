# ET Smoke Rule

## 目的

用于自动化验证 ET 控制台战斗流程是否按正式协议跑通，并在通过后自动收口输出与临时排查痕迹。

## 运行入口

建议统一使用仓库根目录下的脚本，默认读取 `tools/et-battle-smoke.config.json`：

```powershell
powershell -ExecutionPolicy Bypass -File tools/run_et_battle_smoke.ps1
```

如需临时切换参数，可以直接覆盖配置值：

```powershell
powershell -ExecutionPolicy Bypass -File tools/run_et_battle_smoke.ps1 -ConsistencyRuns 1 -SmokeFrames 300
```

如需使用另一套配置，可以传入 `-ConfigPath`：

```powershell
powershell -ExecutionPolicy Bypass -File tools/run_et_battle_smoke.ps1 -ConfigPath tools/et-battle-smoke.config.json
```

## 默认判定规则

Smoke 通过至少需要满足以下条件：

- 配置门禁先通过，能定位并校验 ET smoke 依赖的角色、属性模板、技能、触发器和 gameplay 配置。
- 战斗流程已启动，且进入 battle 状态。
- runtime 已 ready，且具备 state read model 能力。
- runtime 至少产出一次实体快照和一次 runtime 快照。
- 已提交正式输入：移动输入和技能输入。
- 已解析技能目标，并能定位到目标 actor。
- 已解析正式 DTO 快照：ActorSpawn、ActorTransform 和 StateHash。
- ActorSpawn 快照能覆盖本地 actor 与目标 actor。
- 已解析至少一种正式事件快照：Damage / Projectile / Area。
- battle 帧数达到最小门槛，且没有提前失败。

当前默认运行参数来自 `tools/et-battle-smoke.config.json`：

- `SmokeFrames=600`
- `MinBattleFrames=30`
- `TimeoutMilliseconds=15000`
- `SleepMilliseconds=16`
- `DrainFrames=5`
- `ConsistencyRuns=2`
- `SmokeCasePath=tools/et-battle-smoke.case.damage.json`
- `NoBuild=false`
- `SkipConfigValidation=false`
- `KeepOutput=false`

命令行参数优先级高于配置文件，配置文件优先级高于脚本内置兜底值。正式回归建议修改配置文件或新增独立配置文件；临时排查才直接传命令行参数。

## Smoke Case 规则

运行参数配置负责决定“怎么跑”，Smoke Case 配置负责决定“跑什么、期望什么”。默认 case 为 `tools/et-battle-smoke.case.damage.json`，当前覆盖一个基础技能生命周期闭环：移动输入 + 首次技能输入 + 目标受击 + 冷却窗口内二次技能输入 + 正式快照输出。

Smoke Case 当前支持：

- `Name`：用例名称，会写入 `[ETBattleSmoke]` 和 `DeterminismSignature`。
- `Inputs`：输入序列。
- `Inputs[].Type=move`：提交移动输入，使用 `FrameOffset`、`MoveX`、`MoveZ`。
- `Inputs[].Type=skill`：提交技能输入，使用 `FrameOffset`、`SkillSlot`、`Target=current-target`。
- `Expected`：期望结果集合，用于替代代码内置判定。
- `Expected.RequireMoveInput` / `RequireSkillInput`：要求对应输入已提交。
- `Expected.RequireTransformSnapshot` / `RequireStateHashSnapshot` / `RequireActorSpawnSnapshot` / `RequireEventSnapshot`：要求对应正式 DTO 输出已被解析。
- `Expected.MinEventSnapshots` / `MaxEventSnapshots` / `MinActorSpawns`：要求事件和 ActorSpawn 的数量区间；`MaxEventSnapshots=0` 表示不检查上限。
- `Expected.ExpectedTargetHpAtMost`：目标最低血量必须小于等于该值，`0` 表示不检查。
- `Expected.RequireTargetAttributeGroup` / `RequireTargetResourceContainer` / `RequireTargetSkillLoadout` / `MinTargetActiveSkillCount`：要求目标 read-model 具备正式战斗数据。
- `Expected.RequireLocalSkillCooldown` / `ExpectedCooldownSkillSlot` / `MinLocalSkillCooldownRemainingMs`：要求本地 actor 指定技能槽进入冷却，并且采样到的最大剩余冷却时间达到门槛。

后续新增 `movement-only`、`multi-skill`、`long-run` 等用例时，优先新增独立 case 文件，再通过 `SmokeCasePath` 或 `-SmokeCasePath` 切换。

## 一致性规则

脚本默认会独立启动两次 smoke 流程，并比较 `[ETBattleSmoke]` 输出中的 `DeterminismSignature`。通过条件是每次 smoke 都成功，且所有签名完全一致。

`DeterminismSignature` 当前覆盖以下关键结果：

- `BattleFrame`
- `InputTargetFrame`
- `Entities`
- `Snapshots`
- `Transforms`
- `StateHashFrame`
- `StateHash`
- `Events`
- `ActorSpawns`
- `SpawnLocalActor`
- `SpawnTargetActor`
- `TargetHp`
- `CooldownSkill`
- `CooldownRemainingMs`
- `PendingInputs`
- `LocalActor`
- `TargetActor`
- `ElapsedFrames`

如需临时只跑单次流程，可以给脚本传入 `-ConsistencyRuns 1`，或在配置文件中调整 `ConsistencyRuns`。正式回归建议保持默认双跑，优先暴露同输入下状态哈希、事件输出或 actor 映射不稳定的问题。

## 自动退出规则

- smoke 模式默认会在通过后返回退出码 `0`。
- 为避免控制台挂住，smoke 默认会在通过后执行短暂 drain，然后强制退出进程。
- 如果需要保留进程用于人工观察，可以传入 `--smoke-no-force-exit`。
- 如果需要调整总运行时间，优先调 `--smoke-timeout-ms`，而不是单纯拉大帧数。

## 临时日志清理约定

排查过程中允许存在短期诊断日志，但合入前需要清理或降级，以下类型应视为临时输出：

- `[AI-DIAG]` 前缀日志。
- 只用于定位执行链路的 success-path 逐步日志。
- 高频逐帧采样日志。
- 只为确认某个转换器、执行器、handler 已进入的调试输出。

应保留的日志：

- 参数缺失、配置缺失、依赖缺失。
- 解析失败、执行失败、异常堆栈。
- 影响流程判断的 warning/error。

## 配置校验规则

`tools/run_et_battle_smoke.ps1` 默认会在 build 成功后、正式 smoke 前运行 ET App 的 `--validate-config-only` 模式。当前门禁覆盖 smoke 依赖的最小正式配置集合：

- `moba/characters.json` 中存在角色 `1001`，并绑定属性模板 `1001`。
- 角色 `1001` 包含技能 `10010101`、`10010201`、`10010301`。
- `moba/attribute_templates.json` 中存在模板 `1001`，且 `Hp`、`MaxHp`、`MoveSpeed` 为正数。
- 属性模板 `1001` 的 `ActiveSkills` 覆盖 smoke 依赖技能集合。
- `moba/skills.json` 中存在 smoke 技能集合，且每个技能有有效 `CastFlowId`。
- `ability/triggers/skills/trigger_10001.json` 存在、启用，且包含正数 `give_damage` action。
- `moba/gameplays.json` 必须存在并能解析。

配置门禁失败时，脚本会在启动战斗前退出，失败摘要以 `[ETConfigValidation]` 开头。临时排查可以传 `-SkipConfigValidation`，正式回归不建议跳过。

## 脚本行为

`tools/run_et_battle_smoke.ps1` 会：

- 读取 `tools/et-battle-smoke.config.json`，并应用命令行覆盖参数。
- 解析 `SmokeCasePath` 指向的 case 文件，并传给 ET App 的 `--smoke-case=`。
- 先停止残留的 smoke `dotnet` 进程。
- 清理历史 smoke 输出文件。
- 先 build，再运行配置门禁，然后独立运行指定次数的 smoke。
- 收集每次输出到 `src/AbilityKit.Demo.ET.App/smoke-output-run-*.txt`。
- 抽取每次的 `DeterminismSignature` 并进行一致性比较。
- 成功后删除临时 smoke 输出，失败时保留输出便于排查。
- 最后再次兜底停止残留 smoke 进程。
