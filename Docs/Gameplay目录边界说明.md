# Gameplay 目录边界说明

`Unity/Packages/com.abilitykit.demo.moba.runtime/Runtime/Application/Gameplay` 当前已经不是“旧实现堆”，而是 `MOBA` 游戏玩法层的正式运行时边界。

## 目录职责

- `Core/`：玩法生命周期、配置、变量、结果与事件派发的运行时骨架。
- `Rules/`：独立的玩法规则实现，例如时间限制等领域规则。
- `Systems/`：接入世界系统调度的运行时系统入口。
- `Triggering/`：玩法与通用触发器框架之间的适配层，包括事件名、payload 访问器、数值域与校验。

## 当前判断

- `Core/` 中的 `MobaGameplayService`、`MobaGameplayConfigService`、`MobaGameplayVariableService` 属于正式领域服务，不应视为旧式遗留。
- `Triggering/` 已经承担玩法触发事件绑定与校验职责，方向正确。
- `Rules/` 中的规则实现仍可继续正规化，但当前未发现明显的 legacy 分叉或临时残留。

## 后续建议

- 继续检查 `Gameplay` 是否存在与 `Triggering/` 重复的规则或事件定义。
- 若后续新增玩法逻辑，优先放入 `Triggering/` 或明确的领域服务，而不是再扩展杂糅式逻辑。
- 维持 `Core/` 只承载生命周期和状态边界，避免把具体玩法行为继续塞回主服务。
