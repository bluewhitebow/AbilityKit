# Ability-Kit Demo Moba Editor 示例编辑器工具模块开发设计文档

> **阅读对象**：需要维护 Moba 示例配置资产、调试窗口、热重载菜单和帧同步测试工具的编辑器开发者。
>
> **文档目标**：说明该包如何服务 Moba 示例的配置生产和调试工作流。

---

## 一、设计理念

Demo Moba Editor 是最佳实践 Moba 示例的 Unity Editor 工具层。它把配置 ScriptableObject、JSON 导出、配置同步、战斗调试窗口、碰撞 Gizmo、帧同步测试和热重载菜单集中在编辑器包中。

---

## 二、模块边界

负责：

- 定义 Character、Skill、Buff、Projectile、Vfx 等配置 SO。
- 提供配置表资产、导出器、Json 文件夹同步和校验。
- 提供 BattleDebugWindow 及属性、效果、标签、帧同步等面板。
- 提供 CollisionWorld Gizmo 绘制。
- 提供 EditorGameFlowPumpWindow 和 FrameSyncTestWindow。
- 提供 HotReload 菜单和 Unity 日志适配。

不负责：

- 不参与运行时发布。
- 不承载 Moba 共享 DTO，DTO 在 `demo.moba.share`。
- 不负责服务端配置加载。

---

## 三、注意事项

- Editor 包依赖多个运行时示例包，新增工具时应避免反向污染 Runtime。
- 配置导出 JSON 后需要与 Share DTO 字段保持一致。
- BattleDebug 面板是调试视图，不应作为正式 UI 实现。

---

*文档版本：1.0*  
*最后更新：2026-06-05*
