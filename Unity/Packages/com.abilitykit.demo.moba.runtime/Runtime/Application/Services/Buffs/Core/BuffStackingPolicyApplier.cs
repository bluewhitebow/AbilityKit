using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Components;
using AbilityKit.Demo.Moba;

using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Services.Buffs.Runtime;
using AbilityKit.Demo.Moba.Services.Buffs.Presentation;
using AbilityKit.Demo.Moba.Services.Buffs.Triggering;

namespace AbilityKit.Demo.Moba.Services.Buffs.Core {
    /// <summary>
    /// Buff 叠层策略执行器：只修改堆叠数、剩余时间和来源，不处理事件、上下文和持续行为。
    /// </summary>
    internal sealed class BuffStackingPolicyApplier
    {
        /// <summary>
        /// 对已存在运行时应用配置中的叠层/刷新策略，返回 false 表示本次申请被策略忽略。
        /// </summary>
        public bool ApplyToExisting(BuffRuntime existing, BuffMO buff, int sourceActorId, float durationSeconds)
        {
            if (existing == null) return false;
            if (buff == null) return false;

            switch (buff.StackingPolicy)
            {
                case BuffStackingPolicy.IgnoreIfExists:
                    return false;
                case BuffStackingPolicy.Replace:
                    existing.SourceId = sourceActorId;
                    existing.StackCount = 0;
                    existing.Remaining = durationSeconds;
                    AddStack(existing, buff.MaxStacks);
                    return true;
                case BuffStackingPolicy.AddStack:
                    AddStack(existing, buff.MaxStacks);
                    RefreshRemaining(existing, buff.RefreshPolicy, durationSeconds);
                    existing.SourceId = sourceActorId;
                    return true;
                case BuffStackingPolicy.RefreshDuration:
                    RefreshRemaining(existing, buff.RefreshPolicy, durationSeconds);
                    existing.SourceId = sourceActorId;
                    return true;
                case BuffStackingPolicy.None:
                default:
                    return false;
            }
        }

        /// <summary>
        /// 从对象池创建新运行时，并按新实例语义初始化基础状态。
        /// </summary>
        public BuffRuntime CreateNewRuntime(BuffMO buff, int sourceActorId, float durationSeconds)
        {
            var rt = BuffRepository.RentRuntime();
            rt.BuffId = buff.Id;
            rt.Remaining = durationSeconds;
            rt.IntervalRemainingSeconds = 0;
            rt.SourceId = sourceActorId;
            rt.StackCount = 0;
            rt.SourceContextId = 0;

            AddStack(rt, buff.MaxStacks);
            ResetInterval(rt, buff);
            return rt;
        }

        public static void ResetInterval(BuffRuntime rt, BuffMO buff)
        {
            if (rt == null) return;
            if (buff == null) return;

            var intervalRemainingSeconds = buff.IntervalMs > 0 ? buff.IntervalMs / 1000f : 0f;
            rt.IntervalRemainingSeconds = intervalRemainingSeconds;
            if (rt.Continuous != null)
            {
                rt.Continuous.IntervalRemainingSeconds = intervalRemainingSeconds;
            }
        }

        private static void RefreshRemaining(BuffRuntime rt, BuffRefreshPolicy policy, float durationSeconds)
        {
            if (rt == null) return;

            switch (policy)
            {
                case BuffRefreshPolicy.ResetRemaining:
                    rt.Remaining = durationSeconds;
                    return;
                case BuffRefreshPolicy.AddRemaining:
                    rt.Remaining += durationSeconds;
                    return;
                case BuffRefreshPolicy.KeepRemaining:
                case BuffRefreshPolicy.None:
                default:
                    return;
            }
        }

        private static void AddStack(BuffRuntime rt, int maxStacks)
        {
            if (rt == null) return;

            if (maxStacks <= 0) maxStacks = int.MaxValue;
            if (rt.StackCount >= maxStacks) return;

            rt.StackCount++;
        }
    }
}

