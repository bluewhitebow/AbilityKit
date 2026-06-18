#nullable enable

using System;
using System.Collections.Generic;
using AbilityKit.Network.Runtime.Sync;

namespace AbilityKit.Demo.Shooter.View
{
    /// <summary>
    /// 包装玩法无关的框架 <see cref="FastReconnectSession"/>，让 Shooter 恢复层可以驱动它
    /// （审计 §10.4：FastReconnect 的第一个真实消费方）。
    ///
    /// Shooter 仍以自身的快照导入、重放和业务原因分类作为路由依据；该驱动会把每个
    /// <see cref="ShooterClientRecoveryState"/> 迁移映射到框架阶段机
    /// （Connected → Disconnected → {Resuming | AwaitingFullSnapshot} → Recovered），并采集会话发出的统一
    /// <see cref="SyncHealthEvent"/> 流，让框架负责阶段判定与健康遥测，而不需要重新实现恢复逻辑。
    ///
    /// 协调器会容忍会话严格的迁移守卫：任何非法步骤都会被跳过而不是抛出异常，使包装保持纯增量
    /// （设计 §6 回滚说明）。
    /// </summary>
    internal sealed class ShooterFastReconnectDriver
    {
        private readonly FastReconnectSession _session;
        private readonly List<SyncHealthEvent> _events = new List<SyncHealthEvent>();

        public ShooterFastReconnectDriver(int resumeWindowFrames)
        {
            _session = new FastReconnectSession(resumeWindowFrames < 1 ? 1 : resumeWindowFrames);
        }

        /// <summary>当前框架恢复阶段，也是 Shooter 恢复状态的投影目标。</summary>
        public FastReconnectPhase Phase => _session.Phase;

        /// <summary>框架恢复窗口（帧数），用于区分短距离追帧与完整快照恢复。</summary>
        public int ResumeWindowFrames => _session.ResumeWindowFrames;

        /// <summary>自上次 <see cref="ResetEventBuffer"/> 以来累计的健康事件。</summary>
        public IReadOnlyList<SyncHealthEvent> CollectedEvents => _events;

        /// <summary>清空单次操作的健康事件缓冲；每个公共入口点调用。</summary>
        public void ResetEventBuffer()
        {
            _events.Clear();
        }

        /// <summary>
        /// 记录一次常规权威心跳（收到干净快照且没有待处理恢复）。
        /// 发出框架 <see cref="SyncHealthEventKind.SnapshotReceived"/> 事件。
        /// </summary>
        public void Heartbeat(int authoritativeFrame)
        {
            var phase = _session.Phase;
            if (phase == FastReconnectPhase.Connected || phase == FastReconnectPhase.Recovered)
            {
                TryObserve(authoritativeFrame);
            }
        }

        /// <summary>
        /// 将会话推进到与 Shooter 新恢复状态匹配的阶段，每次只执行一个合法框架迁移，并收集发出的健康事件。
        /// </summary>
        public void Reconcile(FastReconnectPhase target, int authoritativeFrame, int gapHint)
        {
            if (target == FastReconnectPhase.Disconnected)
            {
                return;
            }

            var frame = authoritativeFrame < 0 ? 0 : authoritativeFrame;
            var gap = gapHint < 0 ? -gapHint : gapHint;

            for (var guard = 0; guard < 8; guard++)
            {
                var phase = _session.Phase;
                if (PhaseMatches(phase, target))
                {
                    return;
                }

                if (!Step(phase, target, frame, gap))
                {
                    return;
                }
            }
        }

        private bool Step(FastReconnectPhase current, FastReconnectPhase target, int frame, int gap)
        {
            switch (current)
            {
                case FastReconnectPhase.Connected:
                    // 从 Connected 离开并进入任何恢复/Recovered 阶段都要先断开连接。
                    return TryDisconnect();

                case FastReconnectPhase.Recovered:
                    if (target == FastReconnectPhase.Connected)
                    {
                        return TryObserve(frame);
                    }
                    return TryDisconnect();

                case FastReconnectPhase.Disconnected:
                    // 根据 gap 与恢复窗口的关系选择恢复路径，让会话落到 Shooter 已经决定的阶段。
                    return target == FastReconnectPhase.AwaitingFullSnapshot
                        ? TryReconnect(LargeGapFrame(gap))
                        : TryReconnect(SmallGapFrame(gap));

                case FastReconnectPhase.Resuming:
                case FastReconnectPhase.AwaitingFullSnapshot:
                    // 唯一合法出口是完成恢复；后续迭代再根据目标继续走向 Connected 或新的恢复路径。
                    return TryComplete();

                default:
                    return false;
            }
        }

        private int SmallGapFrame(int gap)
        {
            var window = _session.ResumeWindowFrames;
            var bounded = gap;
            if (bounded < 0) bounded = 0;
            if (bounded > window) bounded = window;
            return _session.LastAckedServerFrame + bounded;
        }

        private int LargeGapFrame(int gap)
        {
            var window = _session.ResumeWindowFrames;
            var forced = gap > window ? gap : window + 1;
            return _session.LastAckedServerFrame + forced;
        }

        private bool TryObserve(int frame)
        {
            var safe = frame < _session.LastAckedServerFrame ? _session.LastAckedServerFrame : frame;
            try
            {
                Collect(_session.ObserveServerFrame(safe));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool TryDisconnect()
        {
            try
            {
                Collect(_session.Disconnect());
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool TryReconnect(int currentServerFrame)
        {
            var safe = currentServerFrame < _session.LastAckedServerFrame
                ? _session.LastAckedServerFrame
                : currentServerFrame;
            try
            {
                Collect(_session.Reconnect(safe));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool TryComplete()
        {
            try
            {
                Collect(_session.CompleteRecovery());
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void Collect(in FastReconnectStepReport report)
        {
            var events = report.HealthEvents;
            for (var i = 0; i < events.Count; i++)
            {
                _events.Add(events[i]);
            }
        }

        private static bool PhaseMatches(FastReconnectPhase phase, FastReconnectPhase target)
        {
            return phase == target;
        }
    }
}
