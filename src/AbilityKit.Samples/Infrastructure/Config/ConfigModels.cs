using System.Collections.Generic;

namespace AbilityKit.Samples.Infrastructure.Config
{
    #region 通用配置模型

    /// <summary>
    /// 通用键值对配置
    /// </summary>
    public class KeyValueConfig
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// 命名的数值配置
    /// </summary>
    public class NamedValue
    {
        public string Name { get; set; }
        public float Value { get; set; }
    }

    /// <summary>
    /// 带参数的命名的数值配置
    /// </summary>
    public class NamedValueEx : NamedValue
    {
        public Dictionary<string, float> Params { get; set; } = new();
    }

    #endregion

    #region 状态机配置

    /// <summary>
    /// 状态配置
    /// </summary>
    public class StateConfig
    {
        public string Name { get; set; }
        public string OnEnter { get; set; }
        public string OnLogic { get; set; }
        public string OnExit { get; set; }
        public List<string> Transitions { get; set; } = new();
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    /// <summary>
    /// 状态转换配置
    /// </summary>
    public class TransitionConfig
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Condition { get; set; }
        public float Priority { get; set; }
    }

    /// <summary>
    /// 状态机配置
    /// </summary>
    public class StateMachineConfig
    {
        public string Name { get; set; }
        public string InitialState { get; set; }
        public List<StateConfig> States { get; set; } = new();
        public List<TransitionConfig> Transitions { get; set; } = new();
    }

    #endregion

    #region 技能/管线配置

    /// <summary>
    /// 技能阶段配置
    /// </summary>
    public class PhaseConfig
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public float Duration { get; set; }
        public List<PhaseConfig> Children { get; set; } = new();
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    /// <summary>
    /// 技能配置
    /// </summary>
    public class AbilityConfig
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public float Cooldown { get; set; }
        public float ManaCost { get; set; }
        public List<PhaseConfig> Phases { get; set; } = new();
        public List<string> Conditions { get; set; } = new();
    }

    #endregion

    #region 行为树配置

    /// <summary>
    /// 行为节点配置
    /// </summary>
    public class BehaviorNodeConfig
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
        public List<BehaviorNodeConfig> Children { get; set; } = new();
    }

    /// <summary>
    /// 行为树配置
    /// </summary>
    public class BehaviorTreeConfig
    {
        public string Name { get; set; }
        public BehaviorNodeConfig Root { get; set; }
        public Dictionary<string, object> Blackboard { get; set; } = new();
    }

    #endregion

    #region 战斗实体配置

    /// <summary>
    /// 战斗实体配置
    /// </summary>
    public class BattleEntityConfig
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public float Health { get; set; }
        public float Attack { get; set; }
        public float Defense { get; set; }
        public float Speed { get; set; }
        public Dictionary<string, float> Attributes { get; set; } = new();
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// 战斗配置
    /// </summary>
    public class BattleConfig
    {
        public string Name { get; set; }
        public List<BattleEntityConfig> Allies { get; set; } = new();
        public List<BattleEntityConfig> Enemies { get; set; } = new();
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    #endregion
}
