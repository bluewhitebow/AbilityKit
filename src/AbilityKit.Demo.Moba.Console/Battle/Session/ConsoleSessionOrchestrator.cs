using System;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.Battle.Config;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.Battle.Session
{
    /// <summary>
    /// 会话编排器
    /// 对齐 Unity SessionOrchestrator
    /// 负责会话的启动/停止编排
    /// </summary>
    public sealed class ConsoleSessionOrchestrator : IDisposable
    {
        private readonly ConsoleBattleContext _context;
        private readonly ConsoleSessionHooks _hooks;
        private readonly ConsoleSessionState _state;

        private bool _isRunning;
        private Exception? _lastError;

        public ConsoleSessionOrchestrator(ConsoleBattleContext context, ConsoleSessionHooks hooks, ConsoleSessionState state)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _hooks = hooks ?? throw new ArgumentNullException(nameof(hooks));
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        /// <summary>
        /// 当前是否正在运行
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// 最后发生的错误
        /// </summary>
        public Exception? LastError => _lastError;

        /// <summary>
        /// 开始会话
        /// </summary>
        public void StartSession(BattleStartPlan plan)
        {
            if (_isRunning)
            {
                Platform.Log.Warn("[SessionOrchestrator] Session already running, stopping first...");
                StopSession();
            }

            try
            {
                _lastError = null;

                // 触发会话开始前钩子
                _hooks.InvokeSessionStarting();

                // 初始化上下文
                _context.Plan = plan;
                _context.LocalActorId = 0;
                _context.PlayerCount = plan.MaxPlayerCount;
                _context.LastFrame = 0;
                _context.LogicTimeSeconds = 0d;
                _context.State = BattleState.InMatch;

                // 重置状态
                _state.Reset();
                _state.IsActive = true;
                _state.StartTimeSeconds = 0d;

                _isRunning = true;

                // 触发会话开始钩子
                _hooks.InvokeSessionStarted();

                Platform.Log.System($"[SessionOrchestrator] Session started: World={plan.WorldId}, Mode={plan.SyncMode}");
            }
            catch (Exception ex)
            {
                _lastError = ex;
                _isRunning = false;
                Platform.Log.Error($"[SessionOrchestrator] Failed to start session: {ex.Message}");
                _hooks.InvokeSessionFailed(ex);
            }
        }

        /// <summary>
        /// 停止会话
        /// </summary>
        public void StopSession()
        {
            if (!_isRunning) return;

            try
            {
                // 触发会话停止前钩子
                _hooks.InvokeSessionStopping();

                _isRunning = false;
                _state.Reset();

                // 触发会话停止钩子
                _hooks.InvokeSessionStopped();

                Platform.Log.System("[SessionOrchestrator] Session stopped");
            }
            catch (Exception ex)
            {
                Platform.Log.Error($"[SessionOrchestrator] Error during session stop: {ex.Message}");
            }
        }

        /// <summary>
        /// 标记收到第一帧
        /// </summary>
        public void MarkFirstFrameReceived()
        {
            if (!_state.FirstFrameReceived)
            {
                _state.FirstFrameReceived = true;
                _hooks.InvokeFirstFrameReceived();
                Platform.Log.System("[SessionOrchestrator] First frame received");
            }
        }

        /// <summary>
        /// 更新帧信息
        /// </summary>
        public void UpdateFrame(int frame, double timeSeconds)
        {
            _state.LastFrame = frame;
            _context.LastFrame = frame;
            _context.LogicTimeSeconds = timeSeconds;
        }

        public void Dispose()
        {
            StopSession();
        }
    }
}
