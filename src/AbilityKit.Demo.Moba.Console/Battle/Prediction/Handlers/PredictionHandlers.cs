using System;
using System.Collections.Generic;
using AbilityKit.Ability.StateSync;
using AbilityKit.Ability.StateSync.Prediction;
using IInputCommand = AbilityKit.Ability.StateSync.IInputCommand;
using IPredictionHandler = AbilityKit.Ability.StateSync.Prediction.IPredictionHandler;
using IReadOnlyListSystem = System.Collections.Generic.IReadOnlyList<string>;
using StateSlots = AbilityKit.Ability.StateSync.Prediction.StateSlots;
using Frame = AbilityKit.Ability.StateSync.Frame;
using PredictionStrategy = AbilityKit.Ability.StateSync.PredictionStrategy;
using PredictionResult = AbilityKit.Ability.StateSync.PredictionResult;
using Vector3 = AbilityKit.Ability.StateSync.Vector3;
using Quaternion = AbilityKit.Ability.StateSync.Quaternion;

namespace AbilityKit.Demo.Moba.Console.Battle.Prediction.Handlers;

/// <summary>
/// 槽位名称常量
/// </summary>
public static class SlotNames
{
    // 位置相关
    public const string Position = "position";
    public const string Velocity = "velocity";
    public const string Rotation = "rotation";

    // 战斗相关
    public const string Health = "health";
    public const string MaxHealth = "maxHealth";
    public const string Mana = "mana";
    public const string MaxMana = "maxMana";

    // 冷却前缀
    public const string CooldownPrefix = "cooldown.";

    // 动画
    public const string AnimationState = "animationState";
}

/// <summary>
/// 移动输入
/// 实现 IInputCommand 接口
/// </summary>
public sealed class MoveInput : IInputCommand
{
    public float VelX { get; }
    public float VelZ { get; }
    public float Rotation { get; }

    public MoveInput(float velX, float velZ, float rotation)
    {
        VelX = velX;
        VelZ = velZ;
        Rotation = rotation;
    }

    public static MoveInput FromBytes(byte[] data)
    {
        if (data.Length < 12) return new MoveInput(0, 0, 0);
        var velX = BitConverter.ToSingle(data, 0);
        var velZ = BitConverter.ToSingle(data, 4);
        var rotation = BitConverter.ToSingle(data, 8);
        return new MoveInput(velX, velZ, rotation);
    }
}

/// <summary>
/// 技能释放输入
/// 实现 IInputCommand 接口
/// </summary>
public sealed class SkillInput : IInputCommand
{
    public int SkillId { get; }
    public int TargetId { get; }

    public SkillInput(int skillId, int targetId = 0)
    {
        SkillId = skillId;
        TargetId = targetId;
    }
}

/// <summary>
/// 移动预测处理器
/// </summary>
public sealed class MovementHandler : IPredictionHandler
{
    public string Name => "Movement";
    public PredictionStrategy Strategy => PredictionStrategy.OptimisticWithRollback;
    public IReadOnlyListSystem RequiredSlots => new[] { SlotNames.Position, SlotNames.Velocity, SlotNames.Rotation };

    private readonly float _frameTime = 1.0f / 30.0f;

    public void Predict(IInputCommand input, StateSlots slots, Frame frame)
    {
        if (input is not MoveInput move) return;

        var pos = slots.GetPosition(SlotNames.Position);
        var newPos = new Vector3(
            pos.X + move.VelX * _frameTime,
            pos.Y,
            pos.Z + move.VelZ * _frameTime
        );

        slots.Set(SlotNames.Position, newPos);
        slots.Set(SlotNames.Velocity, new Vector3(move.VelX, 0, move.VelZ));
        slots.Set(SlotNames.Rotation, new Quaternion(0, move.Rotation, 0, 1));

        bool isMoving = move.VelX != 0 || move.VelZ != 0;
        slots.Set(SlotNames.AnimationState, isMoving ? 1 : 0);
    }

    public PredictionResult Validate(StateSlots predicted, StateSlots server)
    {
        var predPos = predicted.GetPosition(SlotNames.Position);
        var servPos = server.GetPosition(SlotNames.Position);

        var dx = predPos.X - servPos.X;
        var dz = predPos.Z - servPos.Z;
        var distError = MathF.Sqrt(dx * dx + dz * dz);

        if (distError < 0.1f) return PredictionResult.Ok();
        if (distError < 1.0f) return PredictionResult.Minor($"dist={distError:F2}");
        if (distError < 5.0f) return PredictionResult.Major($"dist={distError:F2}");
        return PredictionResult.Critical($"dist={distError:F2}");
    }

    public void ApplyServerState(StateSlots server, StateSlots current)
    {
        if (server.Has(SlotNames.Position))
            current.Set(SlotNames.Position, server.GetPosition(SlotNames.Position));
        if (server.Has(SlotNames.Velocity))
            current.Set(SlotNames.Velocity, server.GetPosition(SlotNames.Velocity));
        if (server.Has(SlotNames.Rotation))
            current.Set(SlotNames.Rotation, server.GetQuaternion(SlotNames.Rotation));
        if (server.Has(SlotNames.AnimationState))
            current.Set(SlotNames.AnimationState, server.GetInt(SlotNames.AnimationState));
    }
}

/// <summary>
/// 冷却预测处理器
/// </summary>
public sealed class CooldownHandler : IPredictionHandler
{
    public string Name => "Cooldown";
    public PredictionStrategy Strategy => PredictionStrategy.OptimisticWithRollback;
    public IReadOnlyListSystem RequiredSlots => new[] { SlotNames.CooldownPrefix + "*" };

    private readonly float _frameTime = 1.0f / 30.0f;
    private readonly Func<int, float> _getSkillCooldown;

    public CooldownHandler(Func<int, float>? getSkillCooldown = null)
    {
        _getSkillCooldown = getSkillCooldown ?? (id => id switch
        {
            1 => 5.0f,
            2 => 8.0f,
            3 => 12.0f,
            4 => 30.0f,
            _ => 10.0f
        });
    }

    public void Predict(IInputCommand input, StateSlots slots, Frame frame)
    {
        if (input is SkillInput skill)
        {
            var slotName = $"{SlotNames.CooldownPrefix}{skill.SkillId}";
            var cooldown = _getSkillCooldown(skill.SkillId);
            slots.Set(slotName, cooldown);
        }

        var keysToUpdate = new List<string>(slots.Keys);
        foreach (var key in keysToUpdate)
        {
            if (key.StartsWith(SlotNames.CooldownPrefix))
            {
                var cd = slots.GetFloat(key);
                if (cd > 0)
                {
                    slots.Set(key, Math.Max(0, cd - _frameTime));
                }
            }
        }
    }

    public PredictionResult Validate(StateSlots predicted, StateSlots server)
    {
        foreach (var key in server.Keys)
        {
            if (key.StartsWith(SlotNames.CooldownPrefix))
            {
                var predCd = predicted.GetFloat(key);
                var servCd = server.GetFloat(key);
                if (Math.Abs(predCd - servCd) > 0.1f)
                {
                    return PredictionResult.Major($"cooldown diff for {key}");
                }
            }
        }
        return PredictionResult.Ok();
    }

    public void ApplyServerState(StateSlots server, StateSlots current)
    {
        foreach (var key in server.Keys)
        {
            if (key.StartsWith(SlotNames.CooldownPrefix))
            {
                current.Set(key, server.GetFloat(key));
            }
        }
    }
}

/// <summary>
/// 生命值预测处理器
/// </summary>
public sealed class HealthHandler : IPredictionHandler
{
    public string Name => "Health";
    public PredictionStrategy Strategy => PredictionStrategy.OptimisticWithRollback;
    public IReadOnlyListSystem RequiredSlots => new[] { SlotNames.Health };

    public void Predict(IInputCommand input, StateSlots slots, Frame frame)
    {
        // 生命值通常不预测，只被动接收服务器更新
    }

    public PredictionResult Validate(StateSlots predicted, StateSlots server)
    {
        var predHp = predicted.GetFloat(SlotNames.Health);
        var servHp = server.GetFloat(SlotNames.Health);

        if (Math.Abs(predHp - servHp) > 0.1f)
        {
            return PredictionResult.Critical("health mismatch - damage not predicted");
        }
        return PredictionResult.Ok();
    }

    public void ApplyServerState(StateSlots server, StateSlots current)
    {
        if (server.Has(SlotNames.Health))
            current.Set(SlotNames.Health, server.GetFloat(SlotNames.Health));
        if (server.Has(SlotNames.MaxHealth))
            current.Set(SlotNames.MaxHealth, server.GetFloat(SlotNames.MaxHealth));
    }
}
