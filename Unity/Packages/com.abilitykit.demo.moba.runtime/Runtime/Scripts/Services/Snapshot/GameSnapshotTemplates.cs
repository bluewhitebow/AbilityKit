using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Services.Snapshot
{
    /// <summary>
    /// 【模板】快照提供者基类
    ///
    /// 此文件提供了快照服务的基础模板。
    /// 新建游戏世界层的快照服务时应参考此模板。
    ///
    /// 使用方法:
    /// 1. 继承 GameSnapshotServiceBase
    /// 2. 实现 CollectSnapshotData() 方法
    /// 3. 使用 [WorldService] 特性注册服务
    ///
    /// 参考文档: Docs/SnapshotGuide.md
    /// </summary>
    /// <typeparam name="TPayload">快照负载类型</typeparam>
    public abstract class GameSnapshotServiceBase<TPayload>
        where TPayload : struct
    {
        // 注意：FrameIndex 和 WorldStateSnapshot 的完整定义在 AbilityKit.Ability.FrameSync 命名空间
        // 此模板展示模式，具体实现参考现有的 MobaActorTransformSnapshotService

        /// <summary>
        /// 获取快照类型标识
        /// </summary>
        protected abstract string SnapshotType { get; }

        /// <summary>
        /// 收集快照数据
        /// 子类实现此方法收集需要同步的数据
        /// </summary>
        /// <param name="payload">输出参数，收集到的数据</param>
        /// <returns>是否有数据需要同步</returns>
        protected abstract bool CollectSnapshotData(out TPayload payload);

        /// <summary>
        /// 尝试获取指定帧的快照
        ///
        /// 示例实现:
        /// <code>
        /// private FrameIndex _lastFrame;
        ///
        /// public bool TryGetSnapshot(FrameIndex frame, out WorldStateSnapshot snapshot)
        /// {
        ///     if (frame.Value == _lastFrame.Value) { snapshot = default; return false; }
        ///     _lastFrame = frame;
        ///     if (!CollectSnapshotData(out var payload)) { snapshot = default; return false; }
        ///     snapshot = new WorldStateSnapshot(frame, SnapshotType, payload);
        ///     return true;
        /// }
        /// </code>
        /// </summary>
        /// <param name="frame">帧索引</param>
        /// <param name="snapshot">输出的快照</param>
        /// <returns>是否有快照</returns>
        public bool TryGetSnapshot(object frame, out object snapshot)
        {
            // TODO: 实现具体逻辑
            snapshot = null;
            return false;
        }
    }

    /// <summary>
    /// 【模板】快照路由器基类
    ///
    /// 此文件提供了快照路由器的模板。
    /// 快照路由器聚合多个快照提供者，按优先级返回第一个可用的快照。
    ///
    /// 使用方法:
    /// 1. 继承 GameSnapshotRouterBase
    /// 2. 在构造函数中注册所有快照提供者
    /// 3. 按优先级排序（EnterGame > Spawn > Transform）
    ///
    /// 参考文档: Docs/SnapshotGuide.md
    /// </summary>
    public abstract class GameSnapshotRouterBase
    {
        // 注意：FrameIndex, WorldStateSnapshot, IWorldStateSnapshotProvider 的完整定义
        // 在 AbilityKit.Ability.Host 和 AbilityKit.Ability.FrameSync 命名空间
        // 此模板展示模式，具体实现参考现有的 MobaSnapshotRouter

        /// <summary>
        /// 获取路由器名称（用于日志）
        /// </summary>
        protected abstract string RouterName { get; }

        /// <summary>
        /// 尝试获取指定帧的快照
        /// 按优先级检查每个提供者，返回第一个可用的快照
        ///
        /// 示例实现:
        /// <code>
        /// private readonly List&lt;IWorldStateSnapshotProvider&gt; _providers = new();
        ///
        /// public bool TryGetSnapshot(FrameIndex frame, out WorldStateSnapshot snapshot)
        /// {
        ///     foreach (var p in _providers)
        ///     {
        ///         if (p.TryGetSnapshot(frame, out snapshot)) return true;
        ///     }
        ///     snapshot = default;
        ///     return false;
        /// }
        /// </code>
        /// </summary>
        /// <param name="frame">帧索引</param>
        /// <param name="snapshot">输出的快照</param>
        /// <returns>是否有快照</returns>
        public bool TryGetSnapshot(object frame, out object snapshot)
        {
            // TODO: 实现具体逻辑
            snapshot = null;
            return false;
        }

        /// <summary>
        /// 获取所有快照提供者
        /// 子类按优先级顺序返回提供者列表
        /// </summary>
        protected abstract IEnumerable<object> GetProviders();
    }
}
