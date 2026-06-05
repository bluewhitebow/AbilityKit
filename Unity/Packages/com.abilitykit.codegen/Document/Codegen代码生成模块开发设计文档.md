# Ability-Kit Codegen 代码生成模块开发设计文档

> **阅读对象**：需要维护 AbilityKit 代码生成属性、生成器注册和 DotNet SourceGenerator 的开发者。
>
> **文档目标**：说明 Codegen 包的运行时生成模型和 DotNet 源生成器边界。

---

## 一、设计理念

Codegen 包提供一套轻量代码生成抽象，让运行时/工具层可以通过 Attribute 声明生成需求，再由生成器注册表组织输出文件、诊断信息和模板。

---

## 二、模块边界

负责：

- 定义 `GenerateCodeAttribute`、`GeneratorOutputAttribute`、`RegisterGeneratorAttribute`。
- 定义 `GeneratorContext`、`GenerationResult`、`OutputFile`、`DiagnosticInfo`。
- 提供 `GeneratorRegistry`。
- 提供 `CodeTemplate` 和 `CodeGenUtilities`。
- 包含 DotNet SourceGenerator 项目和已编译 DLL。

不负责：

- 不决定业务生成规则。
- 不自动运行所有生成器。
- 不替代 Protocol Editor 的专用生成流程。

---

## 三、目录结构

| 路径 | 职责 |
|------|------|
| `Runtime/AbilityKit.CodeGen/Attributes` | 代码生成相关 Attribute |
| `Runtime/AbilityKit.CodeGen/Core` | 生成上下文、结果、诊断、输出文件 |
| `Runtime/AbilityKit.CodeGen/Registration` | 生成器注册 |
| `Runtime/AbilityKit.CodeGen/Templates` | 模板抽象 |
| `Runtime/AbilityKit.CodeGen/Utilities` | 辅助工具 |
| `DotNet~/AbilityKit.SourceGenerator` | 源生成器源码 |

---

## 四、注意事项

- `AbilityKit.SourceGenerator.dll` 与 DotNet 源码需要保持版本一致。
- 生成器输出文件应避免覆盖用户手写代码。
- 诊断信息应带上明确位置，方便编辑器或 CI 展示。

---

*文档版本：1.0*  
*最后更新：2026-06-05*
