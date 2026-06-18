using System;
using System.Collections.Generic;

namespace AbilityKit.Triggering.Runtime.RuleScheduler
{
    /// <summary>
    /// 规则调度语义模式。
    /// 面向自然语言规则拆解后的时间意图，而不是具体业务对象。
    /// </summary>
    public enum ERuleScheduleMode : byte
    {
        Immediate = 0,
        Delayed = 1,
        Every = 2,
        WhileActive = 3,
    }

    /// <summary>
    /// 规则调度运行状态。
    /// </summary>
    public enum ERuleScheduleState : byte
    {
        Registered = 0,
        WaitingDelay = 1,
        Running = 2,
        Paused = 3,
        Completed = 4,
        Interrupted = 5,
        Cancelled = 6,
    }

    /// <summary>
    /// 稳定规则调度句柄。
    /// </summary>
    public readonly struct RuleScheduleHandle : IEquatable<RuleScheduleHandle>
    {
        public readonly string DriverId;
        public readonly int InstanceId;
        public readonly int Version;

        public RuleScheduleHandle(string driverId, int instanceId, int version)
        {
            DriverId = driverId;
            InstanceId = instanceId;
            Version = version;
        }

        public bool IsValid => !string.IsNullOrEmpty(DriverId) && InstanceId > 0 && Version > 0;
        public static RuleScheduleHandle Invalid => default;

        public bool Equals(RuleScheduleHandle other)
        {
            return string.Equals(DriverId, other.DriverId, StringComparison.Ordinal)
                && InstanceId == other.InstanceId
                && Version == other.Version;
        }

        public override bool Equals(object obj) => obj is RuleScheduleHandle other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(DriverId, InstanceId, Version);
        public static bool operator ==(RuleScheduleHandle left, RuleScheduleHandle right) => left.Equals(right);
        public static bool operator !=(RuleScheduleHandle left, RuleScheduleHandle right) => !left.Equals(right);
        public override string ToString() => IsValid ? $"RuleSchedule[{DriverId}:{InstanceId}:{Version}]" : "RuleSchedule[Invalid]";
    }

    /// <summary>
    /// 规则调度计划。
    /// </summary>
    public readonly struct RuleSchedulePlan
    {
        public readonly ERuleScheduleMode Mode;
        public readonly float DelayMs;
        public readonly float IntervalMs;
        public readonly int MaxOccurrences;
        public readonly float Speed;
        public readonly string GroupId;
        public readonly string SubjectId;
        public readonly string Label;
        public readonly bool CanBeInterrupted;
        public readonly bool ReplaceExisting;

        public RuleSchedulePlan(
            ERuleScheduleMode mode,
            float delayMs = 0f,
            float intervalMs = 0f,
            int maxOccurrences = 1,
            float speed = 1f,
            string groupId = null,
            string subjectId = null,
            string label = null,
            bool canBeInterrupted = true,
            bool replaceExisting = false)
        {
            Mode = mode;
            DelayMs = Math.Max(0f, delayMs);
            IntervalMs = Math.Max(0f, intervalMs);
            MaxOccurrences = maxOccurrences;
            Speed = speed <= 0f ? 1f : speed;
            GroupId = groupId;
            SubjectId = subjectId;
            Label = label;
            CanBeInterrupted = canBeInterrupted;
            ReplaceExisting = replaceExisting;
        }

        public static RuleSchedulePlan Now(string groupId = null, string subjectId = null, string label = null)
        {
            return new RuleSchedulePlan(ERuleScheduleMode.Immediate, groupId: groupId, subjectId: subjectId, label: label);
        }

        public static RuleSchedulePlan After(float delayMs, string groupId = null, string subjectId = null, string label = null)
        {
            return new RuleSchedulePlan(ERuleScheduleMode.Delayed, delayMs: delayMs, maxOccurrences: 1, groupId: groupId, subjectId: subjectId, label: label);
        }

        public static RuleSchedulePlan Every(float intervalMs, int maxOccurrences = -1, float delayMs = 0f, string groupId = null, string subjectId = null, string label = null)
        {
            return new RuleSchedulePlan(ERuleScheduleMode.Every, delayMs, intervalMs, maxOccurrences, groupId: groupId, subjectId: subjectId, label: label);
        }

        public static RuleSchedulePlan WhileActive(float intervalMs, float delayMs = 0f, string groupId = null, string subjectId = null, string label = null)
        {
            return new RuleSchedulePlan(ERuleScheduleMode.WhileActive, delayMs, intervalMs, -1, groupId: groupId, subjectId: subjectId, label: label);
        }

        public RuleSchedulePlan WithReplacement(bool replaceExisting = true)
        {
            return new RuleSchedulePlan(Mode, DelayMs, IntervalMs, MaxOccurrences, Speed, GroupId, SubjectId, Label, CanBeInterrupted, replaceExisting);
        }

        public RuleSchedulePlan WithSpeed(float speed)
        {
            return new RuleSchedulePlan(Mode, DelayMs, IntervalMs, MaxOccurrences, speed, GroupId, SubjectId, Label, CanBeInterrupted, ReplaceExisting);
        }
    }

    /// <summary>
    /// 规则调度快照。
    /// </summary>
    public readonly struct RuleScheduleSnapshot
    {
        public readonly RuleScheduleHandle Handle;
        public readonly RuleSchedulePlan Plan;
        public readonly ERuleScheduleState State;
        public readonly float ElapsedMs;
        public readonly float LastExecuteMs;
        public readonly int OccurrenceCount;
        public readonly string InterruptReason;

        public RuleScheduleSnapshot(
            RuleScheduleHandle handle,
            RuleSchedulePlan plan,
            ERuleScheduleState state,
            float elapsedMs,
            float lastExecuteMs,
            int occurrenceCount,
            string interruptReason)
        {
            Handle = handle;
            Plan = plan;
            State = state;
            ElapsedMs = elapsedMs;
            LastExecuteMs = lastExecuteMs;
            OccurrenceCount = occurrenceCount;
            InterruptReason = interruptReason;
        }
    }

    /// <summary>
    /// 单次规则调度执行上下文。
    /// </summary>
    public readonly struct RuleScheduleContext
    {
        public readonly RuleScheduleHandle Handle;
        public readonly RuleSchedulePlan Plan;
        public readonly float DeltaTimeMs;
        public readonly float ScaledDeltaMs;
        public readonly float ElapsedMs;
        public readonly int OccurrenceIndex;
        public readonly object UserContext;

        public RuleScheduleContext(
            RuleScheduleHandle handle,
            RuleSchedulePlan plan,
            float deltaTimeMs,
            float scaledDeltaMs,
            float elapsedMs,
            int occurrenceIndex,
            object userContext)
        {
            Handle = handle;
            Plan = plan;
            DeltaTimeMs = deltaTimeMs;
            ScaledDeltaMs = scaledDeltaMs;
            ElapsedMs = elapsedMs;
            OccurrenceIndex = occurrenceIndex;
            UserContext = userContext;
        }
    }

    /// <summary>
    /// 规则调度效果。
    /// </summary>
    public interface IRuleScheduleEffect
    {
        bool CanExecute(in RuleScheduleContext context);
        void Execute(in RuleScheduleContext context);
        void OnCompleted(in RuleScheduleContext context);
        void OnInterrupted(in RuleScheduleContext context, string reason);
    }

    /// <summary>
    /// 规则调度效果基类。
    /// </summary>
    public abstract class RuleScheduleEffectBase : IRuleScheduleEffect
    {
        public virtual bool CanExecute(in RuleScheduleContext context) => true;
        public abstract void Execute(in RuleScheduleContext context);
        public virtual void OnCompleted(in RuleScheduleContext context) { }
        public virtual void OnInterrupted(in RuleScheduleContext context, string reason) { }
    }

    /// <summary>
    /// 委托式规则调度效果。
    /// </summary>
    public sealed class DelegateRuleScheduleEffect : RuleScheduleEffectBase
    {
        private readonly Action<RuleScheduleContext> _execute;
        private readonly Predicate<RuleScheduleContext> _canExecute;
        private readonly Action<RuleScheduleContext> _onCompleted;
        private readonly Action<RuleScheduleContext, string> _onInterrupted;

        public DelegateRuleScheduleEffect(
            Action<RuleScheduleContext> execute,
            Predicate<RuleScheduleContext> canExecute = null,
            Action<RuleScheduleContext> onCompleted = null,
            Action<RuleScheduleContext, string> onInterrupted = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _onCompleted = onCompleted;
            _onInterrupted = onInterrupted;
        }

        public override bool CanExecute(in RuleScheduleContext context) => _canExecute == null || _canExecute(context);
        public override void Execute(in RuleScheduleContext context) => _execute(context);
        public override void OnCompleted(in RuleScheduleContext context) => _onCompleted?.Invoke(context);
        public override void OnInterrupted(in RuleScheduleContext context, string reason) => _onInterrupted?.Invoke(context, reason);
    }

    /// <summary>
    /// 可替换规则调度驱动。
    /// </summary>
    public interface IRuleSchedulerDriver
    {
        string DriverId { get; }
        RuleScheduleHandle Schedule(in RuleSchedulePlan plan, IRuleScheduleEffect effect);
        bool TryGet(RuleScheduleHandle handle, out RuleScheduleSnapshot snapshot);
        IReadOnlyList<RuleScheduleSnapshot> FindByGroup(string groupId);
        IReadOnlyList<RuleScheduleSnapshot> FindBySubject(string subjectId);
        bool Pause(RuleScheduleHandle handle);
        bool Resume(RuleScheduleHandle handle);
        bool Interrupt(RuleScheduleHandle handle, string reason = null);
        bool Cancel(RuleScheduleHandle handle);
        int InterruptGroup(string groupId, string reason = null);
        int CancelGroup(string groupId);
        void Update(float deltaTimeMs, object userContext = null);
        void Clear();
    }

    /// <summary>
    /// 规则调度驱动注册表。
    /// </summary>
    public sealed class RuleSchedulerRegistry
    {
        private readonly Dictionary<string, IRuleSchedulerDriver> _drivers = new Dictionary<string, IRuleSchedulerDriver>(StringComparer.Ordinal);

        public string DefaultDriverId { get; private set; }
        public int DriverCount => _drivers.Count;

        public RuleSchedulerRegistry(IRuleSchedulerDriver defaultDriver = null)
        {
            RegisterDriver(defaultDriver ?? new DefaultRuleSchedulerDriver(), setAsDefault: true);
        }

        public void RegisterDriver(IRuleSchedulerDriver driver, bool setAsDefault = false)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (string.IsNullOrEmpty(driver.DriverId)) throw new ArgumentException("Rule scheduler driver id cannot be empty.", nameof(driver));

            _drivers[driver.DriverId] = driver;
            if (setAsDefault || string.IsNullOrEmpty(DefaultDriverId))
            {
                DefaultDriverId = driver.DriverId;
            }
        }

        public bool TryGetDriver(string driverId, out IRuleSchedulerDriver driver)
        {
            return _drivers.TryGetValue(NormalizeDriverId(driverId), out driver);
        }

        public IRuleSchedulerDriver GetDriver(string driverId = null)
        {
            if (TryGetDriver(driverId, out var driver)) return driver;
            throw new InvalidOperationException($"Rule scheduler driver not registered: {NormalizeDriverId(driverId)}");
        }

        public RuleScheduleHandle Schedule(in RuleSchedulePlan plan, IRuleScheduleEffect effect, string driverId = null)
        {
            return GetDriver(driverId).Schedule(in plan, effect);
        }

        public bool Pause(RuleScheduleHandle handle) => handle.IsValid && TryGetDriver(handle.DriverId, out var driver) && driver.Pause(handle);
        public bool Resume(RuleScheduleHandle handle) => handle.IsValid && TryGetDriver(handle.DriverId, out var driver) && driver.Resume(handle);
        public bool Interrupt(RuleScheduleHandle handle, string reason = null) => handle.IsValid && TryGetDriver(handle.DriverId, out var driver) && driver.Interrupt(handle, reason);
        public bool Cancel(RuleScheduleHandle handle) => handle.IsValid && TryGetDriver(handle.DriverId, out var driver) && driver.Cancel(handle);

        public void Update(float deltaTimeMs, object userContext = null)
        {
            foreach (var driver in _drivers.Values)
            {
                driver.Update(deltaTimeMs, userContext);
            }
        }

        public void Clear()
        {
            foreach (var driver in _drivers.Values)
            {
                driver.Clear();
            }
        }

        private string NormalizeDriverId(string driverId)
        {
            return string.IsNullOrEmpty(driverId) ? DefaultDriverId : driverId;
        }
    }

    /// <summary>
    /// 包内默认规则调度驱动。
    /// </summary>
    public sealed class DefaultRuleSchedulerDriver : IRuleSchedulerDriver
    {
        public const string DefaultId = "abilitykit.default";

        private readonly List<Entry> _entries = new List<Entry>();
        private readonly Dictionary<int, Entry> _entriesById = new Dictionary<int, Entry>();
        private int _nextInstanceId = 1;

        public string DriverId { get; }

        public DefaultRuleSchedulerDriver(string driverId = DefaultId)
        {
            DriverId = string.IsNullOrEmpty(driverId) ? DefaultId : driverId;
        }

        public RuleScheduleHandle Schedule(in RuleSchedulePlan plan, IRuleScheduleEffect effect)
        {
            if (effect == null) throw new ArgumentNullException(nameof(effect));

            if (plan.ReplaceExisting)
            {
                CancelMatching(plan.GroupId, plan.SubjectId);
            }

            var handle = new RuleScheduleHandle(DriverId, _nextInstanceId++, 1);
            var state = plan.DelayMs > 0f ? ERuleScheduleState.WaitingDelay : ERuleScheduleState.Registered;
            var entry = new Entry(handle, plan, effect, state);
            _entries.Add(entry);
            _entriesById[handle.InstanceId] = entry;
            return handle;
        }

        public bool TryGet(RuleScheduleHandle handle, out RuleScheduleSnapshot snapshot)
        {
            if (TryGetEntry(handle, out var entry))
            {
                snapshot = entry.CreateSnapshot();
                return true;
            }

            snapshot = default;
            return false;
        }

        public IReadOnlyList<RuleScheduleSnapshot> FindByGroup(string groupId)
        {
            return Find(entry => string.Equals(entry.Plan.GroupId, groupId, StringComparison.Ordinal));
        }

        public IReadOnlyList<RuleScheduleSnapshot> FindBySubject(string subjectId)
        {
            return Find(entry => string.Equals(entry.Plan.SubjectId, subjectId, StringComparison.Ordinal));
        }

        public bool Pause(RuleScheduleHandle handle)
        {
            if (!TryGetEntry(handle, out var entry) || entry.IsTerminal) return false;
            entry.State = ERuleScheduleState.Paused;
            return true;
        }

        public bool Resume(RuleScheduleHandle handle)
        {
            if (!TryGetEntry(handle, out var entry) || entry.State != ERuleScheduleState.Paused) return false;
            entry.State = entry.Plan.DelayMs > 0f && entry.ElapsedMs < entry.Plan.DelayMs ? ERuleScheduleState.WaitingDelay : ERuleScheduleState.Running;
            return true;
        }

        public bool Interrupt(RuleScheduleHandle handle, string reason = null)
        {
            if (!TryGetEntry(handle, out var entry) || entry.IsTerminal || !entry.Plan.CanBeInterrupted) return false;
            InterruptEntry(entry, reason ?? "Interrupted");
            return true;
        }

        public bool Cancel(RuleScheduleHandle handle)
        {
            if (!TryGetEntry(handle, out var entry) || entry.IsTerminal) return false;
            entry.State = ERuleScheduleState.Cancelled;
            return true;
        }

        public int InterruptGroup(string groupId, string reason = null)
        {
            int count = 0;
            foreach (var entry in _entries)
            {
                if (!entry.IsTerminal && entry.Plan.CanBeInterrupted && string.Equals(entry.Plan.GroupId, groupId, StringComparison.Ordinal))
                {
                    InterruptEntry(entry, reason ?? "Interrupted");
                    count++;
                }
            }
            return count;
        }

        public int CancelGroup(string groupId)
        {
            int count = 0;
            foreach (var entry in _entries)
            {
                if (!entry.IsTerminal && string.Equals(entry.Plan.GroupId, groupId, StringComparison.Ordinal))
                {
                    entry.State = ERuleScheduleState.Cancelled;
                    count++;
                }
            }
            return count;
        }

        public void Update(float deltaTimeMs, object userContext = null)
        {
            var safeDelta = Math.Max(0f, deltaTimeMs);
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var entry = _entries[i];
                if (entry.IsTerminal)
                {
                    RemoveAt(i, entry);
                    continue;
                }

                Tick(entry, safeDelta, userContext);

                if (entry.IsTerminal)
                {
                    RemoveAt(i, entry);
                }
            }
        }

        public void Clear()
        {
            _entries.Clear();
            _entriesById.Clear();
        }

        private void Tick(Entry entry, float deltaTimeMs, object userContext)
        {
            if (entry.State == ERuleScheduleState.Paused) return;

            var scaledDelta = deltaTimeMs * entry.Plan.Speed;
            entry.ElapsedMs += scaledDelta;

            if (entry.State == ERuleScheduleState.WaitingDelay)
            {
                if (entry.ElapsedMs < entry.Plan.DelayMs) return;
                entry.State = ERuleScheduleState.Running;
                entry.LastExecuteMs = entry.ElapsedMs;
            }

            if (entry.State == ERuleScheduleState.Registered)
            {
                entry.State = ERuleScheduleState.Running;
            }

            switch (entry.Plan.Mode)
            {
                case ERuleScheduleMode.Immediate:
                case ERuleScheduleMode.Delayed:
                    ExecuteOnce(entry, deltaTimeMs, scaledDelta, userContext);
                    Complete(entry, deltaTimeMs, scaledDelta, userContext);
                    break;
                case ERuleScheduleMode.Every:
                case ERuleScheduleMode.WhileActive:
                    ExecuteInterval(entry, deltaTimeMs, scaledDelta, userContext);
                    break;
            }
        }

        private void ExecuteInterval(Entry entry, float deltaTimeMs, float scaledDeltaMs, object userContext)
        {
            var interval = entry.Plan.IntervalMs;
            if (interval <= 0f)
            {
                ExecuteOnce(entry, deltaTimeMs, scaledDeltaMs, userContext);
            }
            else if (entry.ElapsedMs - entry.LastExecuteMs >= interval)
            {
                ExecuteOnce(entry, deltaTimeMs, scaledDeltaMs, userContext);
                entry.LastExecuteMs = entry.ElapsedMs;
            }

            if (entry.Plan.MaxOccurrences > 0 && entry.OccurrenceCount >= entry.Plan.MaxOccurrences)
            {
                Complete(entry, deltaTimeMs, scaledDeltaMs, userContext);
            }
        }

        private void ExecuteOnce(Entry entry, float deltaTimeMs, float scaledDeltaMs, object userContext)
        {
            var context = entry.CreateContext(deltaTimeMs, scaledDeltaMs, userContext);
            if (!entry.Effect.CanExecute(in context)) return;

            entry.Effect.Execute(in context);
            entry.OccurrenceCount++;
        }

        private void Complete(Entry entry, float deltaTimeMs, float scaledDeltaMs, object userContext)
        {
            if (entry.IsTerminal) return;
            entry.State = ERuleScheduleState.Completed;
            var context = entry.CreateContext(deltaTimeMs, scaledDeltaMs, userContext);
            entry.Effect.OnCompleted(in context);
        }

        private void InterruptEntry(Entry entry, string reason)
        {
            entry.State = ERuleScheduleState.Interrupted;
            entry.InterruptReason = reason;
            var context = entry.CreateContext(0f, 0f, null);
            entry.Effect.OnInterrupted(in context, reason);
        }

        private void CancelMatching(string groupId, string subjectId)
        {
            if (string.IsNullOrEmpty(groupId) && string.IsNullOrEmpty(subjectId)) return;

            foreach (var entry in _entries)
            {
                if (entry.IsTerminal) continue;
                var groupMatches = string.IsNullOrEmpty(groupId) || string.Equals(entry.Plan.GroupId, groupId, StringComparison.Ordinal);
                var subjectMatches = string.IsNullOrEmpty(subjectId) || string.Equals(entry.Plan.SubjectId, subjectId, StringComparison.Ordinal);
                if (groupMatches && subjectMatches)
                {
                    entry.State = ERuleScheduleState.Cancelled;
                }
            }
        }

        private IReadOnlyList<RuleScheduleSnapshot> Find(Predicate<Entry> predicate)
        {
            var result = new List<RuleScheduleSnapshot>();
            foreach (var entry in _entries)
            {
                if (!entry.IsTerminal && predicate(entry)) result.Add(entry.CreateSnapshot());
            }
            return result;
        }

        private bool TryGetEntry(RuleScheduleHandle handle, out Entry entry)
        {
            if (!handle.IsValid || !string.Equals(handle.DriverId, DriverId, StringComparison.Ordinal))
            {
                entry = null;
                return false;
            }

            if (_entriesById.TryGetValue(handle.InstanceId, out entry) && entry.Handle.Version == handle.Version)
            {
                return true;
            }

            entry = null;
            return false;
        }

        private void RemoveAt(int index, Entry entry)
        {
            _entries.RemoveAt(index);
            _entriesById.Remove(entry.Handle.InstanceId);
        }

        private sealed class Entry
        {
            public readonly RuleScheduleHandle Handle;
            public readonly RuleSchedulePlan Plan;
            public readonly IRuleScheduleEffect Effect;
            public ERuleScheduleState State;
            public float ElapsedMs;
            public float LastExecuteMs;
            public int OccurrenceCount;
            public string InterruptReason;

            public Entry(RuleScheduleHandle handle, RuleSchedulePlan plan, IRuleScheduleEffect effect, ERuleScheduleState state)
            {
                Handle = handle;
                Plan = plan;
                Effect = effect;
                State = state;
            }

            public bool IsTerminal => State == ERuleScheduleState.Completed || State == ERuleScheduleState.Interrupted || State == ERuleScheduleState.Cancelled;

            public RuleScheduleSnapshot CreateSnapshot()
            {
                return new RuleScheduleSnapshot(Handle, Plan, State, ElapsedMs, LastExecuteMs, OccurrenceCount, InterruptReason);
            }

            public RuleScheduleContext CreateContext(float deltaTimeMs, float scaledDeltaMs, object userContext)
            {
                return new RuleScheduleContext(Handle, Plan, deltaTimeMs, scaledDeltaMs, ElapsedMs, OccurrenceCount, userContext);
            }
        }
    }
}
