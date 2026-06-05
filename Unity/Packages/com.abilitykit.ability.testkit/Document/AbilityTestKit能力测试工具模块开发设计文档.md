# Ability-Kit Ability TestKit 能力测试工具模块开发设计文档

> **阅读对象**：需要在 Unity Editor 中编写 Ability/Trigger 相关单元测试和测试夹具的开发者。
>
> **文档目标**：说明 TestKit 包提供的测试替身、内存加载器和触发器测试宿主。

---

## 一、设计理念

Ability TestKit 是 Editor 测试辅助包。它用轻量测试对象替代真实加载器和世界环境，让 Ability、Trigger、等待动作等逻辑可以在编辑器测试中独立验证。

---

## 二、模块边界

负责：

- 提供 `TriggerWorldTestHarness`。
- 提供 `TestWaitTriggerActionFactory`。
- 提供 `InMemoryTextLoader`。
- 提供 Editor 测试程序集 asmdef。

不负责：

- 不参与运行时发布。
- 不提供完整集成测试环境。
- 不替代真实资源加载和网络环境。

---

## 三、注意事项

- 该包位于 Editor/UnitTest，主要用于 Unity Test Runner。
- 测试夹具应保持 deterministic，避免依赖真实时间和外部文件。
- 新增 Ability/Trigger 行为后，可优先在 TestKit 中补测试替身。

---

*文档版本：1.0*  
*最后更新：2026-06-05*
