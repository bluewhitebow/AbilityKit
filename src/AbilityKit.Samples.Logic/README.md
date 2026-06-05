# AbilityKit.Samples.Logic

> AbilityKit 的纯逻辑示例集，用来帮助新人从零到一理解框架理念、模块边界和按需组合方式。

## 示例定位

`AbilityKit.Samples.Logic` 不绑定 Unity、MonoGame 或具体服务器框架。它只保留玩法逻辑、配置模型、上下文、日志和时间推进等纯 C# 内容，运行环境由外层宿主提供。

这意味着同一份 sample 可以：

- 在控制台运行并输出日志。
- 写入文件，方便回归检查和教学留档。
- 未来接入 Unity、MonoGame 或服务器宿主，由宿主提供时间、资源、配置和输出。

## 推荐学习路径

| 阶段 | 分类 | 目标 |
| --- | --- | --- |
| 00 | Onboarding | 理解 AbilityKit 是工具集合，不是必须整包接入的单体框架 |
| 01 | Foundation | 理解日志、事件、对象池、类型注册等基础设施 |
| 02 | Tags / Config | 理解玩法词汇、配置数据和类型映射 |
| 03 | Pipeline / Flow | 理解一次性阶段执行和跨帧流程编排 |
| 04 | Triggering / HFSM / Modifiers | 理解事件规则、状态切换和属性变化 |
| 05 | World | 理解运行时生命周期、服务容器和 Host 边界 |
| 06 | Demo | 阅读端到端示例，学习模块组合方式 |

建议新人先运行 `Onboarding` 分类中的 5 个示例。菜单里的数字只是当前宿主渲染出的临时 index，不应写死到文档或 UI 中；稳定入口应使用 `sample-manifest.json` 中配置的 `id`。

- `onboarding/orientation`：项目定位、解决的问题、阅读 sample 的方法。
- `onboarding/host-boundary`：纯逻辑与控制台、文件、游戏宿主的边界。
- `onboarding/package-composition`：如何根据玩法需求选择模块。
- `onboarding/skill-slice`：把标签、检查、执行、持续效果和事件串成一个技能切片。
- `onboarding/ui-host`：模拟界面宿主列目录、点击运行、收集结构化日志。

## 运行方式

在仓库根目录运行：

```powershell
dotnet run --project src/AbilityKit.Samples -- --list
dotnet run --project src/AbilityKit.Samples -- --id onboarding/orientation
dotnet run --project src/AbilityKit.Samples -- --web sample-web
dotnet run --project src/AbilityKit.Samples -- --all --file --output sample-output
```

常用参数：

| 参数 | 说明 |
| --- | --- |
| `--list` | 打印所有示例菜单 |
| `--run <index>` | 运行指定序号示例 |
| `--id <stable-id>` | 通过 manifest 中的稳定 id 运行指定示例 |
| `--all` | 运行全部示例 |
| `--mode <instant|simulated|realtime>` | 选择时间推进模式 |
| `--file` | 将示例输出写入日志文件 |
| `--output <directory>` | 指定文件输出目录，同时启用 `--file` |
| `--web [directory]` | 导出一个可直接打开的静态网页，不需要持续运行 HTTP 服务 |
| `--no-console` | 不向控制台输出，只保留其他输出通道 |

## 接入带界面的宿主

sample 的纯逻辑层不直接依赖控制台。带界面的宿主可以用 `SampleCatalogProvider` 获取菜单数据，用 `SampleExecutionService` 在按钮点击时运行指定示例。

```csharp
using AbilityKit.Samples.Abstractions;
using AbilityKit.Samples.Logic;

var catalog = SampleCatalogProvider.CreateCatalog();
var executor = new SampleExecutionService(
    catalog,
    mode => MyEnvironmentFactory.Create(mode));

foreach (var entry in catalog.Entries)
{
    // UI 可绑定 entry.Index、entry.Id、entry.Title、entry.Description、entry.Category、entry.Tags
    AddButton(entry.Title, onClick: () =>
    {
        var logger = new BufferedSampleLogger();
        var result = executor.RunById(entry.Id, logger, new SampleRunOptions
        {
            HostKind = SampleHostKind.Web,
            ExecutionMode = ExecutionMode.Simulated
        });

        RenderLog(logger.Entries);
        RenderResult(result.Succeeded, result.ErrorMessage);
    });
}
```

宿主需要自己实现或复用：

- `ILogger`：把日志渲染到 UI 面板、控制台、文件或引擎日志。
- `ISampleEnvironment`：由宿主推进时间，Unity/MonoGame 可以在 Update 中 Tick。
- `IConfigProvider` / `IResourceProvider`：按需要接入文件、内存、Addressables 或远端资源。

## Web 模式与持续驱动

当前先接入的是“静态网页导出模式”。它适合这个仓库当前还是控制台程序的阶段：

```powershell
dotnet run --project src/AbilityKit.Samples -- --web sample-web
```

命令会执行 sample，生成 `sample-web/index.html`。之后可以直接用浏览器打开这个 HTML 文件；改代码后重新执行命令，再刷新浏览器即可，不需要持续开启 HTTP 服务。

这个模式的边界也要明确：浏览器打开本地 `file://` 页面时，不能直接启动本机 .NET 进程，所以网页里的“点击示例”展示的是导出时已经执行并嵌入页面的数据。真正的在线点击即跑，需要 WebAssembly 或后端服务。

如果后续需要真正的网页实时运行，可以升级为以下模式：

- Blazor WebAssembly / .NET WebAssembly：sample 逻辑直接在浏览器侧运行。
- Web 前端 + 后端 .NET：浏览器点击按钮，后端执行 sample 并回传结构化日志。
- Unity WebGL / 其他引擎 Web 构建：由引擎层实现 `ILogger` 和 `ISampleEnvironment`。

对于真正实时运行的 Web/Unity/MonoGame 宿主，持续驱动不应该由 sample 自己创建主循环，而应该由宿主提供：

```csharp
var logger = new BufferedSampleLogger();
var handle = executor.StartById("flow/basics", logger, new SampleRunOptions
{
    HostKind = SampleHostKind.Web,
    ExecutionMode = ExecutionMode.Simulated
});

// Web: requestAnimationFrame / timer
// Unity: Update()
// MonoGame: Game.Update()
handle.Tick(deltaTime);
RenderLog(logger.Entries);
```

也就是说，持续能力来自 `ISampleEnvironment` 和 `SampleRunHandle.Tick(deltaTime)`。Web 宿主可以用 `requestAnimationFrame` 计算 delta，再调用 handle；MonoGame/Unity 则在各自的 Update 中调用。sample 内部只订阅环境 tick 或使用 Flow/Pipeline 的 Step，不直接依赖具体平台。

## 当前接入排查

已经比较明确接入框架包能力的示例：

- `Onboarding/FromConceptToSkillSlice.cs`：`GameplayTags + Pipeline`。
- `Onboarding/SampleHostIntegration.cs`：`SampleCatalogProvider + SampleExecutionService + BufferedSampleLogger`。
- `Flow/FlowBasics.cs`：`AbilityKit.Flow` 的 `FlowRunner / FlowContext / SequenceNode / WaitSecondsNode / ActionNode`。
- `Tags/*`：`AbilityKit.GameplayTags`。
- `Pipeline/*` 和 `Demo/ProgressiveSkill_Phase4.cs`：`AbilityKit.Pipeline`。
- `Modifiers/*` 与 `Continuous/*`：`AbilityKit.Modifiers` / `AbilityKit.Core.Continuous`。
- `World/*`：`AbilityKit.Host` / `AbilityKit.World.DI`。
- `Targeting/TargetingBasics.cs`：`AbilityKit.Combat.Targeting`。

战斗相关 sample 的后续拆分规划见 `Document/CombatSampleCoveragePlan.md`。这份规划按实体索引、技能库、目标查找、伤害、碰撞、运动、投射物、World/同步/回放拆分，后续补示例时优先按其中的最小优先清单推进。

仍建议后续继续清理的旧示例：

- `Flow/SequenceAndRace.cs`、`Flow/TimedFlow.cs`、`Flow/FlowAdvancedExample.cs` 还偏说明文风格，应逐步改成真实节点运行。
- `Triggering/*` 中有一批示例仍偏概念日志，应优先对齐 Unity package 下 `com.abilitykit.triggering/Samples` 的真实 API。
- 早期 `Foundation/EventSystem.cs`、`ObjectPool.cs`、`MarkerRegistry.cs` 需要继续确认是否应直接接入 `Core.Common` 包能力，还是作为 sample 基础设施概念保留。

## 目录结构

```text
AbilityKit.Samples.Logic/
├── Samples/
│   ├── Onboarding/        # 新手导览：框架定位、宿主边界、按需组合、技能切片、UI宿主
│   ├── Foundation/        # 基础设施：HelloWorld、事件、对象池、类型注册
│   ├── Tags/              # GameplayTags、TagContainer、TagRequirements、TagStack
│   ├── Config/            # 配置模型、Attribute 注册、配置表加载
│   ├── Pipeline/          # 技能阶段、执行器、配置驱动 Pipeline
│   ├── Flow/              # 流程节点、Sequence、Race、TimedFlow
│   ├── StateMachine/      # HFSM、行为、触发和配置化状态机
│   ├── Triggering/        # 触发器、条件、Blackboard、调度器、TriggerPlan
│   ├── Continuous/        # 持续行为
│   ├── Modifiers/         # 属性修改器、叠层、衰减、与 HFSM/Continuous 集成
│   ├── Targeting/         # 目标搜索
│   ├── World/             # World 生命周期、DI、Host、客户端管理
│   └── Demo/              # 综合示例和渐进式技能示例
├── Ability/               # 早期技能系统分层示例和测试
├── Infrastructure/        # sample 内部配置模型、资源提供器和注册表辅助代码
└── sample-manifest.json   # 稳定 id、展示顺序、标题和标签配置
```

## 编写新示例的约定

- 示例逻辑继承 `SampleBase`，通过 `[Sample(priority, tags)]` 自动注册。
- 示例只表达纯逻辑，不直接依赖 Unity API、MonoGame API 或控制台静态输出。
- 输出使用 `Log`、`Section`、`Bullet`、`KeyValue` 等方法，方便宿主重定向到控制台或文件。
- 时间推进使用 `Environment` / `AdvanceTime` / `SimulateFrames`，不要在逻辑层直接阻塞线程。
- 复杂示例先说明“它解决什么问题”，再展示“用哪些模块组合解决”。

## 与真实项目的关系

当前仓库仍处于开发期，`src` 下的 sample 目标是教学和验证框架设计。真实项目不需要复制全部示例，也不需要接入所有包；应根据项目需求按需选择 AbilityKit 模块，并参考 `Demo` 与 `demo.moba.*` 的组合方式落地。
