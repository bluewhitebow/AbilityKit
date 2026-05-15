using System;
using System.Collections.Generic;
using Prediction = AbilityKit.Ability.StateSync.Prediction;
using Frame = AbilityKit.Ability.StateSync.Frame;
using Vector3 = AbilityKit.Ability.StateSync.Vector3;

namespace AbilityKit.Demo.Moba.Console.Battle.Prediction.Motion;

/// <summary>
/// 运动描述基类
/// 表现层根据这个描述重建简化版的运动计算
/// </summary>
public abstract class MotionDescriptor
{
    /// <summary>
    /// 运动类型标识
    /// </summary>
    public abstract string MotionType { get; }

    /// <summary>
    /// 开始帧
    /// </summary>
    public int StartFrame { get; set; }

    /// <summary>
    /// 持续帧数（0表示持续运动）
    /// </summary>
    public int DurationFrames { get; set; }

    /// <summary>
    /// 运动结束帧
    /// </summary>
    public int EndFrame => StartFrame + DurationFrames;

    /// <summary>
    /// 是否已结束
    /// </summary>
    public bool IsFinished(Frame currentFrame)
    {
        if (DurationFrames == 0) return false;
        return currentFrame.Value >= EndFrame;
    }
}

/// <summary>
/// 平移运动描述
/// 对应服务器的 LocomotionMotionSource
/// </summary>
public sealed class LocomotionDescriptor : MotionDescriptor
{
    public override string MotionType => "Locomotion";

    /// <summary>
    /// 输入向量（标准化）
    /// </summary>
    public float InputX { get; set; }
    public float InputZ { get; set; }

    /// <summary>
    /// 速度
    /// </summary>
    public float Speed { get; set; }

    /// <summary>
    /// 当前朝向
    /// </summary>
    public float ForwardX { get; set; } = 0;
    public float ForwardZ { get; set; } = 1;

    /// <summary>
    /// 计算给定帧的位置
    /// </summary>
    public Vector3 ComputePosition(
        Vector3 startPos,
        Frame computeFrame,
        float frameTime)
    {
        if (IsFinished(computeFrame))
            return startPos;

        // 计算从开始到当前帧的总时间
        int framesSinceStart = computeFrame.Value - StartFrame;
        float elapsedTime = framesSinceStart * frameTime;

        // 计算方向（与服务器 LocomotionMotionSource 一致）
        var forward = new Vector3(ForwardX, 0, ForwardZ);
        var len = MathF.Sqrt(forward.X * forward.X + forward.Z * forward.Z);
        if (len > 0.00001f)
        {
            forward = new Vector3(forward.X / len, 0, forward.Z / len);
        }
        else
        {
            forward = new Vector3(0, 0, 1);
        }

        var right = new Vector3(forward.Z, 0, -forward.X);
        var direction = new Vector3(
            right.X * InputX + forward.X * InputZ,
            0,
            right.Z * InputX + forward.Z * InputZ
        );

        var dirLen = MathF.Sqrt(direction.X * direction.X + direction.Z * direction.Z);
        if (dirLen <= 0.00001f)
            return startPos;
        direction = new Vector3(direction.X / dirLen, 0, direction.Z / dirLen);

        // 位置 = 起始位置 + 方向 * 速度 * 时间
        return new Vector3(
            startPos.X + direction.X * Speed * elapsedTime,
            startPos.Y,
            startPos.Z + direction.Z * Speed * elapsedTime
        );
    }
}

/// <summary>
/// 路径跟随描述
/// 对应服务器的 PathFollowerMotionSource
/// </summary>
public sealed class PathFollowerDescriptor : MotionDescriptor
{
    public override string MotionType => "PathFollower";

    /// <summary>
    /// 路径点
    /// </summary>
    public List<Vector3> Points { get; set; } = new();

    /// <summary>
    /// 当前路径点索引
    /// </summary>
    public int CurrentIndex { get; set; }

    /// <summary>
    /// 速度
    /// </summary>
    public float Speed { get; set; }

    /// <summary>
    /// 到达阈值
    /// </summary>
    public float ArriveEpsilon { get; set; } = 0.05f;

    /// <summary>
    /// 计算给定帧的位置
    /// </summary>
    public Vector3 ComputePosition(
        Vector3 startPos,
        Frame computeFrame,
        float frameTime)
    {
        if (CurrentIndex >= Points.Count)
            return startPos;

        // 计算时间
        int framesSinceStart = computeFrame.Value - StartFrame;
        float elapsedTime = framesSinceStart * frameTime;

        var pos = startPos;
        var timeRemaining = elapsedTime;

        // 从当前路径点开始追
        for (int i = CurrentIndex; i < Points.Count && timeRemaining > 0; i++)
        {
            var target = Points[i];
            var toTarget = new Vector3(
                target.X - pos.X,
                target.Y - pos.Y,
                target.Z - pos.Z
            );
            var dist = MathF.Sqrt(toTarget.X * toTarget.X + toTarget.Z * toTarget.Z);

            if (dist <= ArriveEpsilon)
            {
                pos = target;
                CurrentIndex = i + 1;
                continue;
            }

            var maxDist = Speed * timeRemaining;
            if (maxDist >= dist)
            {
                // 可以到达这个点
                timeRemaining -= dist / Speed;
                pos = target;
                CurrentIndex = i + 1;
            }
            else
            {
                // 只能走一部分
                var dir = new Vector3(toTarget.X / dist, 0, toTarget.Z / dist);
                pos = new Vector3(
                    pos.X + dir.X * maxDist,
                    pos.Y,
                    pos.Z + dir.Z * maxDist
                );
                timeRemaining = 0;
            }
        }

        return pos;
    }
}

/// <summary>
/// 冲刺描述
/// </summary>
public sealed class DashDescriptor : MotionDescriptor
{
    public override string MotionType => "Dash";

    /// <summary>
    /// 冲刺方向
    /// </summary>
    public float DirectionX { get; set; }
    public float DirectionZ { get; set; }

    /// <summary>
    /// 冲刺距离
    /// </summary>
    public float Distance { get; set; }

    /// <summary>
    /// 冲刺速度
    /// </summary>
    public float Speed { get; set; }

    public Vector3 ComputePosition(
        Vector3 startPos,
        Frame computeFrame,
        float frameTime)
    {
        if (IsFinished(computeFrame))
            return startPos;

        int framesSinceStart = computeFrame.Value - StartFrame;
        float elapsedTime = framesSinceStart * frameTime;

        // 归一化方向
        var dirLen = MathF.Sqrt(DirectionX * DirectionX + DirectionZ * DirectionZ);
        if (dirLen <= 0.00001f)
            return startPos;

        var dir = new Vector3(DirectionX / dirLen, 0, DirectionZ / dirLen);

        // 计算已行进距离
        var timeToFinish = Distance / Speed;
        var actualTime = Math.Min(elapsedTime, timeToFinish);
        var traveled = Speed * actualTime;

        return new Vector3(
            startPos.X + dir.X * traveled,
            startPos.Y,
            startPos.Z + dir.Z * traveled
        );
    }
}
