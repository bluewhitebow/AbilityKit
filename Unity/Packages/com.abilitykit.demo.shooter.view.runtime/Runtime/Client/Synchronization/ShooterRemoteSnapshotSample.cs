#nullable enable

using System;
using System.Collections.Generic;
using AbilityKit.Network.Runtime;

namespace AbilityKit.Demo.Shooter.View
{
    /// <summary>
    /// A remote authoritative actor sample captured from a gateway snapshot for
    /// <see cref="NetworkSyncModel.AuthoritativeInterpolation"/> playback. Stores just the actor
    /// transforms plus the world/frame/server-tick metadata needed to buffer and interpolate them.
    /// The <see cref="TimelineTicks"/> is the authoritative <c>ServerTicks</c> of the snapshot.
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
    /// Builds an interpolated <see cref="ShooterGatewaySnapshot"/> from a pair of bracketing
    /// <see cref="ShooterRemoteSnapshotSample"/> samples so the existing presentation pipeline can
    /// render delayed, smoothly interpolated remote actor state without any rollback.
    ///
    /// This is a stateful, single-consumer projector: it reuses its internal actor list and index
    /// across calls so steady-state playback does not allocate per frame. The produced snapshot is
    /// only valid until the next <see cref="Project"/> call, which is fine because the presentation
    /// pipeline consumes it synchronously and copies out the fields it needs.
    /// </summary>
    public sealed class ShooterRemoteSnapshotProjector
    {
        private readonly List<ShooterGatewayActorSnapshot> _actors = new List<ShooterGatewayActorSnapshot>();
        private readonly Dictionary<int, ShooterGatewayActorSnapshot> _fromById = new Dictionary<int, ShooterGatewayActorSnapshot>();
        private readonly HashSet<int> _emitted = new HashSet<int>();

        /// <summary>
        /// Produces a snapshot whose actors are linearly interpolated between the two bracketing
        /// samples by <paramref name="interpolation"/>.Alpha. Actors present in both samples blend
        /// position/rotation/velocity/hp; actors present only in one side are handled so that newly
        /// spawned remotes appear at their authoritative pose and despawning remotes hold their last
        /// pose through the in-between frame instead of popping out mid-interpolation.
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

            // Emit every target actor, blending with its prior pose when present (spawned-only actors
            // pass through at the authoritative pose).
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

            // Carry actors that exist in 'from' but not in 'to' (despawned between samples). Holding
            // their last pose for the in-between frame avoids a one-frame flicker; the next sample's
            // target set will drop them once playback advances past 'to'.
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
                // Rotation is an angle (radians, derived from the aim vector). Blend it along the
                // shortest arc so values straddling the ±π seam do not spin the long way around.
                InterpolationMath.LerpAngleRadians(from.Rotation, to.Rotation, alpha),
                InterpolationMath.Lerp(from.VelocityX, to.VelocityX, alpha),
                InterpolationMath.Lerp(from.VelocityY, to.VelocityY, alpha),
                InterpolationMath.Lerp(from.Hp, to.Hp, alpha),
                to.HpMax,
                to.TeamId);
        }
    }
}
