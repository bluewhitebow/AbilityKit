# Ability-Kit HotReload 热更新运行时模块开发设计文档

> **阅读对象**：需要在运行期替换系统实现、重载配置或调试热修复入口的开发者。
>
> **文档目标**：说明 HotReload 包的入口、静态注册、服务覆盖和日志边界。

---

## 一、设计理念

HotReload 包提供轻量热更新/热修复抽象。它不绑定某个热更框架，而是通过 entry、proxy、overlay、static registry 和 logger 组织可替换实现。

---

## 二、模块边界

负责：

- 定义 `IHotfixEntry` 热修复入口。
- 定义 `IHotfixLogger`。
- 提供 `HotReloadRuntime`。
- 提供 `HotfixSystemProxy` 和 `HotfixServiceOverlay`。
- 提供 `HotReloadStaticAttribute` 与 `HotReloadStaticRegistry`。
- 提供配置重载调试监听器。

不负责：

- 不加载外部 DLL。
- 不实现 ILRuntime/HybridCLR 等具体热更技术。
- 不保证所有系统可安全替换，替换策略由业务决定。

---

## 三、使用建议

- 将热更入口实现为 `IHotfixEntry`。
- 用 `IHotfixLogger` 接入 Unity 或服务端日志。
- 用 overlay 包装可替换服务，避免业务代码直接持有旧实现。
- 静态注册只用于明确可热替换的入口，不应扫描所有类型。

---

## 四、注意事项

- 热替换涉及生命周期，旧服务释放和新服务初始化必须成对设计。
- 静态状态容易残留，重载前后需要明确清理策略。
- 该包偏运行时抽象，具体热更平台适配应独立成包。

---

*文档版本：1.0*  
*最后更新：2026-06-05*
