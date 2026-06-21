# 5.4 配置系统

> 理解配置加载、验证和热更新机制。

---

## 目录

1. [配置系统概述](#1-配置系统概述)
2. [配置加载](#2-配置加载)
3. [配置验证](#3-配置验证)
4. [热更新](#4-热更新)

---

## 1. 配置系统概述

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           配置系统概述                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   配置系统 = 游戏数据的加载、验证和管理                              │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐ │
│   │  设计目标                                                            │ │
│   │  • 策划可配置，无需修改代码                                         │ │
│   │  • 运行时可热更新                                                  │ │
│   │  • 启动时验证配置正确性                                             │ │
│   └─────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. 配置加载

```csharp
public interface IConfigLoader
{
    /// <summary>加载所有配置</summary>
    void LoadAll();

    /// <summary>加载指定配置</summary>
    TConfig Load<TConfig>(string path) where TConfig : class;

    /// <summary>获取配置</summary>
    TConfig Get<TConfig>() where TConfig : class;
}

// JSON 配置加载
public class JsonConfigLoader : IConfigLoader
{
    public TConfig Load<TConfig>(string path) where TConfig : class
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<TConfig>(json);
    }
}

// 使用
var loader = new JsonConfigLoader();
var characters = loader.Load<CharacterConfig[]>("Configs/moba/characters.json");
var skills = loader.Load<SkillConfig[]>("Configs/moba/skills.json");
```

---

## 3. 配置验证

```csharp
public readonly struct ValidationResult
{
    public bool IsValid { get; }
    public string[] Errors { get; }

    public static ValidationResult Success() => new(true, Array.Empty<string>());
    public static ValidationResult Fail(params string[] errors) => new(false, errors);
}

public interface IConfigValidator<TConfig>
{
    ValidationResult Validate(TConfig config);
}

// 技能配置验证
public sealed class SkillConfigValidator : IConfigValidator<SkillConfig>
{
    public ValidationResult Validate(SkillConfig config)
    {
        var errors = new List<string>();

        if (config.SkillId <= 0)
            errors.Add($"Invalid SkillId: {config.SkillId}");

        if (config.Cooldown < 0)
            errors.Add($"Cooldown cannot be negative: {config.Cooldown}");

        if (config.ManaCost < 0)
            errors.Add($"ManaCost cannot be negative: {config.ManaCost}");

        if (config.CastRange <= 0)
            errors.Add($"CastRange must be positive: {config.CastRange}");

        if (errors.Count > 0)
            return ValidationResult.Fail(errors.ToArray());

        return ValidationResult.Success();
    }
}
```

---

## 4. 热更新

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           热更新流程                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   1. 加载新配置                                                         │
│      newConfig = LoadConfig("skills_v2.json")                            │
│                                                                             │
│   2. 验证新配置                                                         │
│      if (!validator.Validate(newConfig)) return Error                    │
│                                                                             │
│   3. 原子替换                                                           │
│      Interlocked.Exchange(ref _currentConfig, newConfig)               │
│                                                                             │
│   4. 清理旧配置                                                         │
│      await Task.Delay(1000) // 等待所有引用释放                       │
│      oldConfig.Dispose()                                                 │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 下一步

- [对象池](./02-ObjectPool.md) - 性能优化
- [事件系统](./01-EventSystem.md) - 发布订阅模式

---

*文档版本：v1.0 | 最后更新：2026-06-21*
