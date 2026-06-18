#nullable enable

using System;

namespace AbilityKit.Network.Runtime.Sync
{
    /// <summary>
    /// 单次同步 tick 中发出的玩法无关同步健康信号。Carrier 会将它与 <see cref="SyncReconciliationReport"/> 一起暴露，
    /// 让 DemoHarness 可以聚合统一的健康视图（快照流、插值、恢复、输入、验证），而不是继续扩展校正报告。
    /// 该事件刻意保持轻量：包含类别、严重级别、关联帧，以及一个含义由类别决定的数值负载
    /// （例如重放 tick 数、丢弃快照数、重映射帧）。
    /// </summary>
    public readonly struct SyncHealthEvent : IEquatable<SyncHealthEvent>
    {
        public SyncHealthEvent(SyncHealthEventKind kind, SyncHealthSeverity severity, int frame = 0, long value = 0L)
        {
            if (frame < 0) throw new ArgumentOutOfRangeException(nameof(frame));

            Kind = kind;
            Severity = severity;
            Frame = frame;
            Value = value;
        }

        public SyncHealthEventKind Kind { get; }

        public SyncHealthSeverity Severity { get; }

        public int Frame { get; }

        public long Value { get; }

        public bool HasEvent => Kind != SyncHealthEventKind.None;

        public static SyncHealthEvent None { get; } = default;

        /// <summary>创建指定类别的 <see cref="SyncHealthSeverity.Info"/> 事件。</summary>
        public static SyncHealthEvent Info(SyncHealthEventKind kind, int frame = 0, long value = 0L)
        {
            return new SyncHealthEvent(kind, SyncHealthSeverity.Info, frame, value);
        }

        /// <summary>创建指定类别的 <see cref="SyncHealthSeverity.Warning"/> 事件。</summary>
        public static SyncHealthEvent Warning(SyncHealthEventKind kind, int frame = 0, long value = 0L)
        {
            return new SyncHealthEvent(kind, SyncHealthSeverity.Warning, frame, value);
        }

        /// <summary>创建指定类别的 <see cref="SyncHealthSeverity.Error"/> 事件。</summary>
        public static SyncHealthEvent Error(SyncHealthEventKind kind, int frame = 0, long value = 0L)
        {
            return new SyncHealthEvent(kind, SyncHealthSeverity.Error, frame, value);
        }

        public bool Equals(SyncHealthEvent other)
        {
            return Kind == other.Kind &&
                   Severity == other.Severity &&
                   Frame == other.Frame &&
                   Value == other.Value;
        }

        public override bool Equals(object? obj)
        {
            return obj is SyncHealthEvent other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Kind, Severity, Frame, Value);
        }

        public static bool operator ==(SyncHealthEvent left, SyncHealthEvent right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SyncHealthEvent left, SyncHealthEvent right)
        {
            return !left.Equals(right);
        }
    }
}
