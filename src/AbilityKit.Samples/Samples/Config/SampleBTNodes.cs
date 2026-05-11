using AbilityKit.Samples.Infrastructure.Config.Attributes;

namespace AbilityKit.Samples.Samples.Config
{
    /// <summary>
    /// 选择器节点 - 依次执行子节点，返回第一个成功的
    /// </summary>
    [BTNodeTypeId("Selector")]
    public sealed class SelectorBTNode { }

    /// <summary>
    /// 序列节点 - 依次执行子节点，返回第一个失败的
    /// </summary>
    [BTNodeTypeId("Sequence")]
    public sealed class SequenceBTNode { }

    /// <summary>
    /// 条件节点 - 执行条件检查
    /// </summary>
    [BTNodeTypeId("Condition")]
    public sealed class ConditionBTNode { }

    /// <summary>
    /// 动作节点 - 执行具体动作
    /// </summary>
    [BTNodeTypeId("Action")]
    public sealed class ActionBTNode { }

    /// <summary>
    /// 并行节点 - 同时执行所有子节点
    /// </summary>
    [BTNodeTypeId("Parallel")]
    public sealed class ParallelBTNode { }

    /// <summary>
    /// 循环节点 - 重复执行子节点
    /// </summary>
    [BTNodeTypeId("Loop")]
    public sealed class LoopBTNode { }
}
