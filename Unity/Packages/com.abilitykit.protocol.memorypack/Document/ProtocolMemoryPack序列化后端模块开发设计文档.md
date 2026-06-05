# Ability-Kit Protocol MemoryPack 序列化后端模块开发设计文档

> **阅读对象**：需要把 AbilityKit Protocol 的二进制序列化切换到 MemoryPack 的客户端/服务端开发者。
>
> **文档目标**：说明该包作为可选序列化后端的职责、安装方式和依赖边界。

---

## 一、设计理念

`com.abilitykit.protocol.memorypack` 是 Protocol 核心包的可选后端。核心包只定义 `IWireSerializer`，该包提供 MemoryPack 实现，并通过安装器把它设置为当前序列化器。

这种拆分让不同项目可以按需组合：不需要 MemoryPack 的项目只使用核心协议包，需要高性能二进制协议的项目再加入该包。

---

## 二、模块边界

负责：

- 实现 `AbilityKit.Protocol.Serialization.IWireSerializer`。
- 通过反射调用 `MemoryPack.MemoryPackSerializer.Serialize<T>` 和 `Deserialize<T>`。
- 提供安装器设置全局 serializer。

不负责：

- 不提供 MemoryPack 源码或 NuGet 安装流程。
- 不定义业务协议结构。
- 不负责 OpCode 注册。
- 不负责文本 Json 序列化。

---

## 三、目录结构

| 路径 | 职责 |
|------|------|
| `Runtime/Serialization/MemoryPackWireSerializer.cs` | MemoryPack 二进制序列化器 |
| `Runtime/Serialization/MemoryPackWireSerializerInstaller.cs` | 安装到 `WireSerializer.Current` 的入口 |
| `Runtime/Plugins.rar` | 当前包内预留/压缩插件资源 |
| `Runtime/com.abilitykit.protocol.memorypack.asmdef` | 引用 `AbilityKit.Protocol` 和 `MemoryPack` |

---

## 四、核心实现

`MemoryPackWireSerializer` 在静态初始化时查找 `MemoryPack.MemoryPackSerializer`：

1. 先使用 `Type.GetType("MemoryPack.MemoryPackSerializer")`。
2. 再扫描当前 AppDomain 中已加载程序集。
3. 找不到时保留 null，调用 Serialize/Deserialize 时抛出明确异常。

序列化时会查找泛型静态方法：

- `Serialize<T>(T value)` 或 by-ref 形式。
- `Deserialize<T>(byte[] bytes)`。

`Deserialize(ReadOnlySpan<byte>)` 当前通过 `ToArray()` 转为 byte[]，因此不是零拷贝路径。

---

## 五、使用流程

```csharp
using AbilityKit.Protocol.MemoryPack;

MemoryPackWireSerializerInstaller.Install();

var bytes = WireSerializer.Serialize(in message);
var value = WireSerializer.Deserialize<MyMessage>(bytes);
```

如果项目使用 `ProtocolRegistry.SetSerializer` 作为入口，也需要显式把同一个 serializer 设置给 Registry，避免全局 `WireSerializer.Current` 与 Registry 内部 serializer 不一致。

---

## 六、注意事项

- `package.json` 当前没有声明对 `com.abilitykit.protocol` 或 MemoryPack 的依赖，但 asmdef 已引用 `AbilityKit.Protocol` 和 `MemoryPack`；按需组合时需要补齐依赖来源。
- 反射查找方法对 MemoryPack API 签名较敏感，升级 MemoryPack 后需要验证。
- 当前 `Runtime/Plugins.rar` 不会被 Unity 当作 DLL 自动引用，若压缩包内是插件，需要解压到可引用路径。
- 协议类型需要按 MemoryPack 要求声明 `[MemoryPackable]` 和可序列化字段。

---

*文档版本：1.0*  
*最后更新：2026-06-05*
