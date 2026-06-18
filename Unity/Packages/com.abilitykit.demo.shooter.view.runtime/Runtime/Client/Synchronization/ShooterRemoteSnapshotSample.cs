#nullable enable

using System;
using System.Collections.Generic;
using AbilityKit.Network.Runtime;

namespace AbilityKit.Demo.Shooter.View
{
    /// <summary>
    /// 从网关快照采集的远端权威 actor 样本，用于 <see cref="NetworkSyncModel.AuthoritativeInterpolation"/> 播放。
    /// 只保存 actor 变换，以及缓冲和插值所需的世界、帧与服务器 tick 元数据。
    /// <see cref="TimelineTicks"/> 对应快照的权威 <c>ServerTicks</c>。
    /// </summary>
    public sealed class ShooterRemoteSnapshotSample : IRemoteSnapshotSample
    {
        public ShooterRemoteSnapshotSample(ulong worldId, int frame, long serverTicks, IReadOnlyList<ShooterGatewayActorSnapshot> actors)
        {
            WorldId = worldId;
            Frame = frame;
            ServerTicks = serverTicks;
            Actors = actors ?? Array.Empty<ShooterGatewayActorSnapshot>();
        }

        public ulong WorldId { get; }

        public int Frame { get; }

        public long ServerTicks { get; }

        public IReadOnlyList<ShooterGatewayActorSnapshot> Actors { get; }

        public long TimelineTicks => ServerTicks;
    }

    /// <summary>
    /// 基于一对包围播放时间点的 <see cref="ShooterRemoteSnapshotSample"/> 样本构建插值后的
    /// <see cref="ShooterGatewaySnapshot"/>，让现有表现管线无需回滚即可渲染延迟且平滑插值的远端 actor 状态。
    ///
    /// 这是一个有状态、单消费者的投影器：它会跨调用复用内部 actor 列表与索引，避免稳定播放期逐帧分配。
    /// 产出的快照只在下一次 <see cref="Project"/> 调用前有效；表现管线会同步消费并拷贝所需字段，因此这是安全的。
    /// </summary>
    public sealed class ShooterRemoteSnapshotProjector
    {
        private readonly List<ShooterGatewayActorSnapshot> _actors = new List<ShooterGatewayActorSnapshot>();
        private readonly Dictionary<int, ShooterGatewayActorSnapshot> _fromById = new Dictionary<int, ShooterGatewayActorSnapshot>();
        private readonly HashSet<int> _emitted = new HashSet<int>();

        /// <summary>
        /// 生成 actor 按 <paramref name="interpolation"/>.Alpha 在两个包围样本之间线性插值的快照。
        /// 两个样本都存在的 actor 会混合位置、旋转、速度和生命值；只存在于一侧的 actor 会特殊处理：
        /// 新生成的远端对象直接以权威姿态出现，消失中的远端对象则在中间帧保持最后姿态，避免插值中途闪烁消失。
        /// </summary>
        public ShooterGatewaySnapshot Project(in RemoteSnapshotInterpolation<ShooterRemoteSnapshotSample> interpolation)
        {
            var from = interpolation.From;
            var to = interpolation.To;
            float alpha = interpolation.Alpha;

            if (ReferenceEquals(from, to) || alpha <= 0f)
            {
                return BuildSnapshot(from, from.Actors);
            }

            if (alpha >= 1f)
            {
                return BuildSnapshot(to, to.Actors);
            }

            _actors.Clear();
            _emitted.Clear();
            BuildIndex(from.Actors);

            // 输出每个目标 actor；若存在上一姿态则混合，否则新生成 actor 直接使用权威姿态。
            for (int i = 0; i < to.Actors.Count; i++)
            {
                var target = to.Actors[i];
                if (_fromById.TryGetValue(target.ActorId, out var source))
                {
                    _actors.Add(Lerp(in source, in target, alpha));
                }
                else
                {
                    _actors.Add(target);
                }

                _emitted.Add(target.ActorId);
            }

            // 保留存在于 from 但不存在于 to 的 actor（两个样本之间消失）。
            // 中间帧保持最后姿态可避免单帧闪烁；播放越过 to 后，下一个样本的目标集合会移除它们。
            for (int i = 0; i < from.Actors.Count; i++)
            {
                var source = from.Actors[i];
                if (!_emitted.Contains(source.ActorId))
                {
                    _actors.Add(source);
                }
            }

            return BuildSnapshot(to, _actors);
        }

        private static ShooterGatewaySnapshot BuildSnapshot(ShooterRemoteSnapshotSample meta, IReadOnlyList<ShooterGatewayActorSnapshot> actors)
        {
            return new ShooterGatewaySnapshot(
                meta.WorldId,
                meta.Frame,
                0d,
                meta.ServerTicks,
                isFullSnapshot: true,
                actors,
                payloadOpCode: 0,
                packedSnapshot: null);
        }

        private void BuildIndex(IReadOnlyList<ShooterGatewayActorSnapshot> actors)
        {
            _fromById.Clear();
            for (int i = 0; i < actors.Count; i++)
            {
                _fromById[actors[i].ActorId] = actors[i];
            }
        }

        private static ShooterGatewayActorSnapshot Lerp(in ShooterGatewayActorSnapshot from, in ShooterGatewayActorSnapshot to, float alpha)
        {
            return new ShooterGatewayActorSnapshot(
                to.ActorId,
                InterpolationMath.Lerp(from.X, to.X, alpha),
                InterpolationMath.Lerp(from.Y, to.Y, alpha),
                // Rotation 是由瞄准向量导出的弧度角。沿最短弧混合，避免跨 ±π 接缝时绕远路旋转。
                InterpolationMath.LerpAngleRadians(from.Rotation, to.Rotation, alpha),
                InterpolationMath.Lerp(from.VelocityX, to.VelocityX, alpha),
                InterpolationMath.Lerp(from.VelocityY, to.VelocityY, alpha),
                InterpolationMath.Lerp(from.Hp, to.Hp, alpha),
                to.HpMax,
                to.TeamId);
        }
    }
}
