using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Console.Flow
{
    /// <summary>
    /// 阶段上下�?
    /// </summary>
    public sealed class PhaseContext
    {
        /// <summary>
        /// 根上下文
        /// </summary>
        public IModuleContext Root { get; set; }

        /// <summary>
        /// 阶段名称
        /// </summary>
        public string PhaseName { get; set; }

        /// <summary>
        /// 上一阶段
        /// </summary>
        public string PreviousPhase { get; set; }

        /// <summary>
        /// 进入时间
        /// </summary>
        public double EnterTime { get; set; }

        /// <summary>
        /// 额外数据
        /// </summary>
        public Dictionary<string, object> Data { get; } = new();
    }

    /// <summary>
    /// 阶段接口
    /// </summary>
    public interface IPhase
    {
        string Name { get; }
        void OnEnter(PhaseContext context);
        void OnTick(PhaseContext context, float deltaTime);
        void OnExit(PhaseContext context, string nextPhase);
    }

    /// <summary>
    /// 阶段主机
    /// </summary>
    public sealed class PhaseHost : IDisposable
    {
        private readonly Dictionary<string, IPhase> _phases = new();
        private readonly List<string> _phaseOrder = new();
        private string _currentPhase;
        private PhaseContext _context;
        private bool _running;

        /// <summary>
        /// 注册阶段
        /// </summary>
        public void Register(IPhase phase)
        {
            if (phase == null) return;
            _phases[phase.Name] = phase;
            if (!_phaseOrder.Contains(phase.Name))
            {
                _phaseOrder.Add(phase.Name);
            }
        }

        /// <summary>
        /// 设置初始阶段
        /// </summary>
        public void SetInitialPhase(string phaseName)
        {
            if (!_phases.ContainsKey(phaseName))
            {
                Platform.Log.Error($"[PhaseHost] Phase not found: {phaseName}");
                return;
            }
            _currentPhase = phaseName;
        }

        /// <summary>
        /// 启动阶段主机
        /// </summary>
        public void Start(PhaseContext context)
        {
            _context = context ?? new PhaseContext();
            _context.Root = context?.Root;
            _running = true;

            if (!string.IsNullOrEmpty(_currentPhase) && _phases.TryGetValue(_currentPhase, out var phase))
            {
                Platform.Log.Phase($"[PhaseHost] Starting with phase: {_currentPhase}");
                phase.OnEnter(_context);
            }
        }

        /// <summary>
        /// 切换阶段
        /// </summary>
        public void TransitionTo(string phaseName)
        {
            if (!_running)
            {
                Platform.Log.Warn("[PhaseHost] Cannot transition: host is not running");
                return;
            }

            if (!_phases.ContainsKey(phaseName))
            {
                Platform.Log.Error($"[PhaseHost] Phase not found: {phaseName}");
                return;
            }

            var previousPhase = _currentPhase;

            if (_phases.TryGetValue(_currentPhase, out var current))
            {
                Platform.Log.Phase($"[PhaseHost] Exiting phase: {_currentPhase}");
                current.OnExit(_context, phaseName);
            }

            _currentPhase = phaseName;
            _context.PreviousPhase = previousPhase;
            _context.EnterTime = Environment.TickCount / 1000.0;

            if (_phases.TryGetValue(_currentPhase, out var next))
            {
                Platform.Log.Phase($"[PhaseHost] Entering phase: {_currentPhase}");
                next.OnEnter(_context);
            }
        }

        /// <summary>
        /// Tick 当前阶段
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!_running || string.IsNullOrEmpty(_currentPhase)) return;

            if (_phases.TryGetValue(_currentPhase, out var phase))
            {
                phase.OnTick(_context, deltaTime);
            }
        }

        /// <summary>
        /// 停止阶段主机
        /// </summary>
        public void Stop()
        {
            if (!_running) return;

            if (_phases.TryGetValue(_currentPhase, out var current))
            {
                Platform.Log.Phase($"[PhaseHost] Stopping phase: {_currentPhase}");
                current.OnExit(_context, null);
            }

            _running = false;
            _currentPhase = null;
        }

        /// <summary>
        /// 获取当前阶段名称
        /// </summary>
        public string CurrentPhase => _currentPhase;

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning => _running;

        public void Dispose()
        {
            Stop();
            _phases.Clear();
            _phaseOrder.Clear();
            _context = null;
        }
    }
}
