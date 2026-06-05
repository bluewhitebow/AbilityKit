# Ability-Kit BaseEditor 基础编辑器工具模块开发设计文档

> **阅读对象**：需要维护 AbilityKit Unity 编辑器窗口、预览器和通用编辑器组件的开发者。
>
> **文档目标**：说明 BaseEditor 包作为编辑器基础工具集合的职责，以及它与具体运行时包的关系。

---

## 一、包定位

BaseEditor 是 Unity Editor 工具集合，包含可插拔窗口框架、动作编辑器预览能力、对象池监控窗口和早期 GameplayTag 管理工具。它服务于编辑器工作流，不参与运行时逻辑。

---

## 二、模块边界

负责：

- 提供 `PlugableWindow`、WindowBuilder、窗口组件、事件和插件基础。
- 提供动作编辑器预览初始化器、Sampler 和 Clip 预览实现。
- 提供 PoolMonitorWindow。
- 提供 GameplayTag 编辑器相关窗口和导出工具。

不负责：

- 不定义运行时游戏规则。
- 不负责真实技能执行。
- 不负责服务端逻辑和协议。

---

## 三、目录结构

| 路径 | 职责 |
|------|------|
| `Editor/Framework` | 可插拔窗口框架 |
| `Editor/ActionEditorImpl` | 动作编辑器预览和初始化 |
| `Editor/ActionEditorImpl/Preview/Sampler` | Animation、Audio、Model、Particle、Spine 采样 |
| `Editor/ActionEditorImpl/Preview/Clips` | 片段预览实现 |
| `Editor/PoolExtension` | 对象池监控 |
| `Editor/GamplayTag` | 标签编辑器工具 |

---

## 四、注意事项

- 该包 asmdef 是 Editor 程序集，不应被 Runtime asmdef 引用。
- GameplayTag 编辑器代码与 `com.abilitykit.gameplaytags` Editor 目录存在职责重叠，后续应收敛到一个主实现。
- 预览器只模拟表现效果，不能代替真实运行时执行结果。

---

*文档版本：1.0*  
*最后更新：2026-06-05*
