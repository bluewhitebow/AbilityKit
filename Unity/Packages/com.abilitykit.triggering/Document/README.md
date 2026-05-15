# Triggering — 事件触发引擎

## 设计理念

Triggering 回答的问题是："当战斗事件发生时，应该执行哪些规则？"

Triggering 是一个**强类型、事件驱动的规则引擎**。触发器通过 `EventKey<TArgs>` 订阅事件，事件发布时 TriggerRunner 按 **Phase→Priority→Order** 三级排序依次评估条件并执行逻辑。ExecCtx（执行上下文）通过依赖注入向所有 Evaluate/Execute 调用传递所需服务，确保规则逻辑与具体实现解耦。

**与 Pipeline 的关系**：Pipeline 编排技能的执行步骤，Triggering 处理步骤内的事件响应。两者是互补的协作关系，而非替代关系。

## 核心抽象

```
TriggerRunner<TCtx>
├── RegisterTrigger()           → 注册 ITrigger
├── Dispatch<TEvent>()          → 触发事件
└── 按 Phase→Priority→Order 排序执行

ExecCtx<TCtx>
├── EventBus                   → 发布/订阅事件
├── FunctionRegistry            → 条件函数（可扩展）
├── ActionRegistry              → 执行动作（可扩展）
├── BlackboardResolver          → 黑板数据（跨触发器共享）
├── NumericDomains              → 数值比较域
└── ExecutionControl           → 短路控制（StopPropagation / Cancel）

ITrigger<TArgs, TCtx>
├── Evaluate()                  → 评估条件
└── Execute()                  → 执行逻辑（可附加 ITriggerCue）

MarkerAttribute 注册
├── [ExecutableTypeId]         → 标记 IExecutable 实现
└── [ConditionTypeId]         → 标记 ICondition 实现
```

## 内置条件与动作

**条件**：`AndCondition`、`OrCondition`、`NotCondition`、`NumericCompareCondition`、`PayloadCompareCondition`、`HasTargetCondition`、`ConstCondition`

**动作**：通过 `ExecutableRegistry` 的 Marker 扫描自动发现，零侵入扩展。

## 快速示例

```csharp
// 定义触发器
public class OnDamageReceivedTrigger : Trigger<DamageEvent>
{
    protected override void Execute(DamageEvent evt, ExecCtx ctx)
    {
        // 应用伤害
        ctx.Source.Health -= evt.Damage;

        // 触发特效
        ctx.EventBus.Publish(new DamageAppliedEvent(evt.Source, evt.Damage));
    }
}

// 通过 TriggerPlan 配置（数据驱动）
var plan = new TriggerPlan<DamageEvent>
{
    Phase = TriggerPhase.Reactive,
    Priority = 100,
    Predicate = PredicateExpr.And(
        NumericCompare(nameof(DamageEvent.Damage), ">", 50),
        PayloadCompare(nameof(DamageEvent.Source.Tag), "==", "Enemy")
    ),
    ActionCalls = new[]
    {
        new ActionCallConfig { ActionKind = "ApplySlow" }
    }
};
```

## 确定性保证

`ExecPolicy.RequireDeterministic` 模式下，Triggering 在注册时拒绝非确定性函数（随机数、网络请求、时间戳等），确保帧同步回放安全可靠。
