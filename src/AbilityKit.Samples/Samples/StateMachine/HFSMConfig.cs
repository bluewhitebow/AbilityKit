using System;
using System.Collections.Generic;
using UnityHFSM;

namespace AbilityKit.Samples.Samples.StateMachine
{
    /// <summary>
    /// HFSM 配置数据模型 - 对应 JSON 配置文件
    /// </summary>
    public sealed class HFSMConfig
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public string Description { get; set; } = string.Empty;
        public List<StateConfig> States { get; set; } = new();
        public List<TransitionConfig> Transitions { get; set; } = new();
        public string InitialState { get; set; } = string.Empty;
        public Dictionary<string, ParameterConfig> Parameters { get; set; } = new();
    }

    public sealed class StateConfig
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = "State";
        public string Description { get; set; } = string.Empty;
        public bool NeedsExitTime { get; set; } = false;
        public ActionConfig OnEnter { get; set; }
        public ActionConfig OnLogic { get; set; }
        public ActionConfig OnExit { get; set; }
    }

    public sealed class TransitionConfig
    {
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public ConditionConfig Condition { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool ForceInstantly { get; set; } = false;
        public float Delay { get; set; } = 0f;
    }

    public sealed class ConditionConfig
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public object Value { get; set; }
        public string Compare { get; set; } = "==";
        public float Duration { get; set; } = 0f;
    }

    public sealed class ActionConfig
    {
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public float Duration { get; set; } = 0f;
        public List<ActionConfig> Children { get; set; } = new();
    }

    public sealed class ParameterConfig
    {
        public string Type { get; set; } = string.Empty;
        public object DefaultValue { get; set; }
    }

    /// <summary>
    /// HFSM 参数管理器 - 管理状态机的运行时参数
    /// </summary>
    public sealed class HFSMParameterManager
    {
        private readonly Dictionary<string, object> _parameters = new();

        public void Set(string name, object value)
        {
            _parameters[name] = value;
        }

        public T Get<T>(string name, T defaultValue = default)
        {
            if (_parameters.TryGetValue(name, out var value))
            {
                if (value is T typedValue)
                    return typedValue;
            }
            return defaultValue;
        }

        public bool GetBool(string name) => Get(name, false);
        public float GetFloat(string name) => Get(name, 0f);
        public int GetInt(string name) => Get(name, 0);

        public bool EvaluateCondition(ConditionConfig condition)
        {
            if (condition == null) return true;

            switch (condition.Type)
            {
                case "Parameter":
                    return EvaluateParameterCondition(condition);
                case "TimeElapsed":
                    return false; // 由 TransitionAfter 处理
                default:
                    return true;
            }
        }

        private bool EvaluateParameterCondition(ConditionConfig condition)
        {
            if (!_parameters.TryGetValue(condition.Name, out var currentValue))
                return false;

            var targetValue = condition.Value;
            var compare = condition.Compare ?? "==";

            switch (compare)
            {
                case "==":
                    return Equals(currentValue, targetValue);
                case "!=":
                    return !Equals(currentValue, targetValue);
                case ">":
                    return CompareNumbers(currentValue, targetValue) > 0;
                case ">=":
                    return CompareNumbers(currentValue, targetValue) >= 0;
                case "<":
                    return CompareNumbers(currentValue, targetValue) < 0;
                case "<=":
                    return CompareNumbers(currentValue, targetValue) <= 0;
                default:
                    return false;
            }
        }

        private int CompareNumbers(object a, object b)
        {
            var aVal = Convert.ToSingle(a);
            var bVal = Convert.ToSingle(b);
            return aVal.CompareTo(bVal);
        }
    }

    /// <summary>
    /// HFSM 配置构建器 - 根据配置构建状态机
    /// </summary>
    public sealed class HFSMConfigBuilder
    {
        private readonly HFSMConfig _config;
        private readonly HFSMParameterManager _parameters;
        private readonly Dictionary<string, State<string, string>> _states = new();
        private readonly Dictionary<string, StateConfig> _stateConfigs = new();
        private readonly Action<string> _logAction;

        public HFSMConfigBuilder(HFSMConfig config, Action<string> logAction = null)
        {
            _config = config;
            _logAction = logAction ?? (_ => { });
            _parameters = new HFSMParameterManager();

            // 初始化参数
            foreach (var kvp in config.Parameters)
            {
                _parameters.Set(kvp.Key, kvp.Value.DefaultValue);
            }
        }

        public HFSMParameterManager Parameters => _parameters;

        public UnityHFSM.StateMachine Build()
        {
            var fsm = new UnityHFSM.StateMachine();

            // 1. 创建所有状态
            foreach (var stateConfig in _config.States)
            {
                var state = CreateState(stateConfig);
                _states[stateConfig.Id] = state;
                _stateConfigs[stateConfig.Id] = stateConfig;
                fsm.AddState(stateConfig.Id, state);
            }

            // 2. 设置初始状态
            if (!string.IsNullOrEmpty(_config.InitialState))
            {
                fsm.SetStartState(_config.InitialState);
            }

            // 3. 创建转换
            foreach (var transitionConfig in _config.Transitions)
            {
                CreateTransition(fsm, transitionConfig);
            }

            return fsm;
        }

        private State<string, string> CreateState(StateConfig config)
        {
            // 创建 onEnter 委托
            Action<State<string, string>> onEnter = null;
            if (config.OnEnter != null)
            {
                onEnter = s => ExecuteAction(config.OnEnter, s);
            }

            // 创建 onLogic 委托
            Action<State<string, string>> onLogic = null;
            if (config.OnLogic != null)
            {
                onLogic = s => ExecuteAction(config.OnLogic, s);
            }

            // 创建 onExit 委托
            Action<State<string, string>> onExit = null;
            if (config.OnExit != null)
            {
                onExit = s => ExecuteAction(config.OnExit, s);
            }

            var state = new State<string, string>(
                onEnter: onEnter,
                onLogic: onLogic,
                onExit: onExit,
                canExit: null,
                needsExitTime: config.NeedsExitTime
            );

            return state;
        }

        private void ExecuteAction(ActionConfig action, State<string, string> state)
        {
            if (action == null) return;

            switch (action.Type)
            {
                case "Log":
                    _logAction($"[{state.name}] {action.Message}");
                    break;

                case "Composite":
                case "Sequence":
                    foreach (var child in action.Children)
                    {
                        ExecuteAction(child, state);
                    }
                    break;

                case "Wait":
                    // 在实际状态机中由 TransitionAfter 处理
                    break;

                default:
                    _logAction($"[{state.name}] 执行动作: {action.Type}");
                    break;
            }
        }

        private void CreateTransition(UnityHFSM.StateMachine fsm, TransitionConfig config)
        {
            // 判断是普通转换还是延迟转换
            if (config.Delay > 0)
            {
                var transition = new TransitionAfter<string>(
                    from: config.From,
                    to: config.To,
                    delay: config.Delay,
                    condition: t => EvaluateCondition(config.Condition),
                    onTransition: null,
                    afterTransition: null,
                    forceInstantly: config.ForceInstantly
                );
                fsm.AddTransition(transition);
            }
            else
            {
                var transition = new Transition<string>(
                    from: config.From,
                    to: config.To,
                    condition: t => EvaluateCondition(config.Condition),
                    onTransition: null,
                    afterTransition: null,
                    forceInstantly: config.ForceInstantly
                );
                fsm.AddTransition(transition);
            }
        }

        private bool EvaluateCondition(ConditionConfig condition)
        {
            if (condition == null) return true;
            return _parameters.EvaluateCondition(condition);
        }
    }
}
