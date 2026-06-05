# Ability-Kit Analyzer 静态约束分析模块开发设计文档

> **阅读对象**：需要维护 AbilityKit 命名空间约束、Roslyn 分析器和 Unity 编辑器检查工具的开发者。
>
> **文档目标**：说明 Analyzer 包的运行时诊断模型、Roslyn 插件、Unity Editor 检查入口和配置边界。

---

## 一、设计理念

Analyzer 包用于约束包之间的引用和命名空间使用，降低模块边界被无意穿透的风险。它同时包含 Unity 可用的诊断模型和 DotNet/Roslyn 分析器源码。

---

## 二、模块边界

负责：

- 定义 `AKDiagnostic`、descriptor、severity、category、location。
- 提供 analyzer rule 注册和 reporter。
- 提供 namespace constraint 配置加载。
- 提供 Unity Editor 检查窗口、BuildChecker、PostProcessor。
- 提供 Roslyn ForbiddenNamespaceAnalyzer 和已编译插件 DLL。

不负责：

- 不自动修复代码。
- 不替代 C# 编译器。
- 不定义所有项目规范，只提供约束框架。

---

## 三、目录结构

| 路径 | 职责 |
|------|------|
| `Runtime/AbilityKit.Analyzer/Core` | 诊断基础模型 |
| `Runtime/AbilityKit.Analyzer/Configuration` | 包约束配置 |
| `Runtime/AbilityKit.Analyzer/Rules` | 命名空间约束规则描述 |
| `Runtime/AbilityKit.Analyzer/Registration` | 规则注册 |
| `Runtime/AbilityKit.Analyzer/Reporting` | 诊断输出 |
| `Editor/ConstraintSettings` | Unity 编辑器配置和检查入口 |
| `DotNet~/AbilityKit.Analyzer` | Roslyn 分析器源码和构建产物 |

---

## 四、注意事项

- `DotNet~/bin/Debug` 下存在构建产物，应确认是否需要纳入包源码或改为构建输出。
- `AbilityKit.Analyzer.Plugin.dll` 是 Roslyn 插件，升级源码后需要同步重建。
- 约束配置应尽量和 package 依赖关系保持一致。

---

*文档版本：1.0*  
*最后更新：2026-06-05*
