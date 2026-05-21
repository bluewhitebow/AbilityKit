using System;
using AbilityKit.Demo.Moba.Console.View;

namespace AbilityKit.Demo.Moba.Console.Battle.Session
{
    /// <summary>
    /// 会话钩子接口
    /// 对齐 Unity BattleSessionHooks
    /// 提供会话生命周期的回调点
    /// </summary>
    public sealed class ConsoleSessionHooks : IDisposable
    {
        private Action? _onPlanBuilt;
        private Action? _onSessionStarting;
        private Action? _onSessionStarted;
        private Action? _onSessionStopping;
        private Action? _onSessionStopped;
        private Action<Exception>? _onSessionFailed;
        private Action<float>? _onPreTick;
        private Action<float>? _onPostTick;
        private Action? _onFirstFrameReceived;

        // ViewBinder Ready Hooks - 对齐 Unity
        private Action<ViewBinderReadyEvent>? _onViewBinderReady;
        private Action<ViewsReboundEvent>? _onViewsRebound;
        private Action<ViewFrameAlignedEvent>? _onViewFrameAligned;

        public event Action? PlanBuilt
        {
            add => _onPlanBuilt += value;
            remove => _onPlanBuilt -= value;
        }

        public event Action? SessionStarting
        {
            add => _onSessionStarting += value;
            remove => _onSessionStarting -= value;
        }

        public event Action? SessionStarted
        {
            add => _onSessionStarted += value;
            remove => _onSessionStarted -= value;
        }

        public event Action? SessionStopping
        {
            add => _onSessionStopping += value;
            remove => _onSessionStopping -= value;
        }

        public event Action? SessionStopped
        {
            add => _onSessionStopped += value;
            remove => _onSessionStopped -= value;
        }

        public event Action<Exception>? SessionFailed
        {
            add => _onSessionFailed += value;
            remove => _onSessionFailed -= value;
        }

        public event Action<float>? PreTick
        {
            add => _onPreTick += value;
            remove => _onPreTick -= value;
        }

        public event Action<float>? PostTick
        {
            add => _onPostTick += value;
            remove => _onPostTick -= value;
        }

        public event Action? FirstFrameReceived
        {
            add => _onFirstFrameReceived += value;
            remove => _onFirstFrameReceived -= value;
        }

        // ViewBinder Ready Hooks - 对齐 Unity

        public event Action<ViewBinderReadyEvent>? ViewBinderReady
        {
            add => _onViewBinderReady += value;
            remove => _onViewBinderReady -= value;
        }

        public event Action<ViewsReboundEvent>? ViewsRebound
        {
            add => _onViewsRebound += value;
            remove => _onViewsRebound -= value;
        }

        public event Action<ViewFrameAlignedEvent>? ViewFrameAligned
        {
            add => _onViewFrameAligned += value;
            remove => _onViewFrameAligned -= value;
        }

        public void InvokePlanBuilt() => _onPlanBuilt?.Invoke();
        public void InvokeSessionStarting() => _onSessionStarting?.Invoke();
        public void InvokeSessionStarted() => _onSessionStarted?.Invoke();
        public void InvokeSessionStopping() => _onSessionStopping?.Invoke();
        public void InvokeSessionStopped() => _onSessionStopped?.Invoke();
        public void InvokeSessionFailed(Exception ex) => _onSessionFailed?.Invoke(ex);
        public void InvokePreTick(float deltaTime) => _onPreTick?.Invoke(deltaTime);
        public void InvokePostTick(float deltaTime) => _onPostTick?.Invoke(deltaTime);
        public void InvokeFirstFrameReceived() => _onFirstFrameReceived?.Invoke();

        // ViewBinder Ready Hooks 触发方法

        public void InvokeViewBinderReady(ViewBinderReadyEvent evt) => _onViewBinderReady?.Invoke(evt);
        public void InvokeViewsRebound(ViewsReboundEvent evt) => _onViewsRebound?.Invoke(evt);
        public void InvokeViewFrameAligned(ViewFrameAlignedEvent evt) => _onViewFrameAligned?.Invoke(evt);

        public void Dispose()
        {
            _onPlanBuilt = null;
            _onSessionStarting = null;
            _onSessionStarted = null;
            _onSessionStopping = null;
            _onSessionStopped = null;
            _onSessionFailed = null;
            _onPreTick = null;
            _onPostTick = null;
            _onFirstFrameReceived = null;

            _onViewBinderReady = null;
            _onViewsRebound = null;
            _onViewFrameAligned = null;
        }
    }

    /// <summary>
    /// ViewBinder 就绪事件参数
    /// 对齐 Unity ViewBinderReadyEvent
    /// </summary>
    public readonly struct ViewBinderReadyEvent
    {
        public IConsoleViewBinder Binder { get; }
        public int FrameIndex { get; }

        public ViewBinderReadyEvent(IConsoleViewBinder binder, int frameIndex)
        {
            Binder = binder;
            FrameIndex = frameIndex;
        }
    }

    /// <summary>
    /// Views 重新绑定事件参数
    /// 对齐 Unity ViewsReboundEvent
    /// </summary>
    public readonly struct ViewsReboundEvent
    {
        public int FrameIndex { get; }

        public ViewsReboundEvent(int frameIndex)
        {
            FrameIndex = frameIndex;
        }
    }

    /// <summary>
    /// 视图帧对齐事件参数
    /// 对齐 Unity ViewFrameAlignedEvent
    /// </summary>
    public readonly struct ViewFrameAlignedEvent
    {
        public int FrameIndex { get; }
        public double TimeSeconds { get; }

        public ViewFrameAlignedEvent(int frameIndex, double timeSeconds)
        {
            FrameIndex = frameIndex;
            TimeSeconds = timeSeconds;
        }
    }
}
