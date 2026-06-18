using System;
using AbilityKit.Protocol.Shooter;
using AbilityKit.World.Svelto;
using Svelto.ECS;
using Svelto.ECS.Internal;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public sealed class ShooterPackedSnapshotExporter
    {
        private readonly ShooterBattleState _state;
        private readonly IShooterEntityManager _entities;
        private readonly IShooterBattleRules _rules;
        private readonly IShooterStateHashProvider _stateHashProvider;
        private readonly ISveltoWorldContext _context;

        public ShooterPackedSnapshotExporter(
            ShooterBattleState state,
            IShooterEntityManager entities,
            IShooterBattleRules rules,
            IShooterStateHashProvider stateHashProvider)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _stateHashProvider = stateHashProvider ?? throw new ArgumentNullException(nameof(stateHashProvider));
            _context = _entities.SveltoContext;
        }

        public ShooterPackedSnapshotPayload Export(ulong worldId, bool isFullSnapshot = true, bool authorityOverride = false)
        {
            var componentChunks = ExportComponentChunks();

            return new ShooterPackedSnapshotPayload(
                ShooterPackedSnapshotCodec.CurrentVersion,
                worldId,
                _state.CurrentFrame,
                _state.CurrentFrame,
                CreateSnapshotFlags(isFullSnapshot, authorityOverride),
                _stateHashProvider.ComputeStateHash(),
                _entities.PlayerCount + _entities.ProjectileCount + CountEnemies(),
                Array.Empty<byte>(),
                componentChunks);
        }

        private ShooterPackedComponentChunk[] ExportComponentChunks()
        {
            return new[]
            {
                ExportPlayerLifecycleChunk(),
                ExportProjectileLifecycleChunk(),
                ExportEnemyLifecycleChunk(),
                ExportPlayerTransformChunk(),
                ExportProjectileTransformChunk(),
                ExportEnemyTransformChunk(),
                ExportPlayerHealthChunk(),
                ExportEnemyHealthChunk(),
                ExportPlayerScoreChunk(),
                ExportProjectileLifetimeChunk()
            };
        }

        private ShooterPackedComponentChunk ExportPlayerLifecycleChunk()
        {
            if (_entities.PlayerCount == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.EntityLifecycle, ShooterPackedEntityKinds.Player);
            }

            var entityIds = ShooterRuntimeSnapshotUtility.CopyAndSort(_entities.PlayerIds);
            var flags = new byte[entityIds.Length];
            var ownerIds = new int[entityIds.Length];
            for (int i = 0; i < entityIds.Length; i++)
            {
                _entities.TryGetPlayer(entityIds[i], out var player);
                flags[i] = (byte)ShooterPackedEntityFlags.Player;
                if (player.Alive)
                {
                    flags[i] |= ShooterPackedEntityFlags.Alive;
                }

                ownerIds[i] = player.PlayerId;
            }

            return new ShooterPackedComponentChunk(
                ShooterPackedComponentKinds.EntityLifecycle,
                ShooterPackedEntityKinds.Player,
                entityIds.Length,
                entityIds,
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<int>(),
                flags,
                ownerIds,
                Array.Empty<int>());
        }

        private ShooterPackedComponentChunk ExportProjectileLifecycleChunk()
        {
            if (_entities.ProjectileCount == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.EntityLifecycle, ShooterPackedEntityKinds.Projectile);
            }

            var entityIds = ShooterRuntimeSnapshotUtility.CopyAndSort(_entities.ProjectileIds);
            var flags = new byte[entityIds.Length];
            var ownerIds = new int[entityIds.Length];
            for (int i = 0; i < entityIds.Length; i++)
            {
                _entities.TryGetProjectile(entityIds[i], out var bullet);
                flags[i] = (byte)(ShooterPackedEntityFlags.Alive | ShooterPackedEntityFlags.Projectile);
                ownerIds[i] = bullet.OwnerPlayerId;
            }

            return new ShooterPackedComponentChunk(
                ShooterPackedComponentKinds.EntityLifecycle,
                ShooterPackedEntityKinds.Projectile,
                entityIds.Length,
                entityIds,
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<int>(),
                flags,
                ownerIds,
                Array.Empty<int>());
        }

        private ShooterPackedComponentChunk ExportEnemyLifecycleChunk()
        {
            var (_, healths, ids, count) = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>(ShooterSveltoGroups.GameplayTargets);
            if (count == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.EntityLifecycle, ShooterPackedEntityKinds.Enemy);
            }

            var order = CreateSortedEnemyOrder(ids, count);
            var entityIds = new int[count];
            var flags = new byte[count];
            for (int i = 0; i < count; i++)
            {
                var sourceIndex = order[i];
                entityIds[i] = (int)ids[sourceIndex];
                flags[i] = (byte)ShooterPackedEntityFlags.Enemy;
                if (healths[sourceIndex].Alive != 0)
                {
                    flags[i] |= ShooterPackedEntityFlags.Alive;
                }
            }

            return new ShooterPackedComponentChunk(
                ShooterPackedComponentKinds.EntityLifecycle,
                ShooterPackedEntityKinds.Enemy,
                entityIds.Length,
                entityIds,
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<int>(),
                flags,
                Array.Empty<int>(),
                Array.Empty<int>());
        }

        private ShooterPackedComponentChunk ExportPlayerTransformChunk()
        {
            if (_entities.PlayerCount == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.Transform, ShooterPackedEntityKinds.Player);
            }

            var entityIds = ShooterRuntimeSnapshotUtility.CopyAndSort(_entities.PlayerIds);
            var posX = new float[entityIds.Length];
            var posY = new float[entityIds.Length];
            var velX = new float[entityIds.Length];
            var velY = new float[entityIds.Length];
            var facingX = new float[entityIds.Length];
            var facingY = new float[entityIds.Length];
            for (int i = 0; i < entityIds.Length; i++)
            {
                _entities.TryGetPlayer(entityIds[i], out var player);
                posX[i] = player.X;
                posY[i] = player.Y;
                facingX[i] = player.AimX;
                facingY[i] = player.AimY;

                if (_state.LatestCommands.TryGetValue(player.PlayerId, out var command))
                {
                    var moveX = command.MoveX;
                    var moveY = command.MoveY;
                    if (ShooterBattleMath.Normalize(ref moveX, ref moveY) > 0f)
                    {
                        velX[i] = moveX * _rules.PlayerSpeed;
                        velY[i] = moveY * _rules.PlayerSpeed;
                    }
                }
            }

            return new ShooterPackedComponentChunk(
                ShooterPackedComponentKinds.Transform,
                ShooterPackedEntityKinds.Player,
                entityIds.Length,
                entityIds,
                posX,
                posY,
                facingX,
                facingY,
                Array.Empty<int>(),
                Array.Empty<byte>(),
                Array.Empty<int>(),
                ShooterPackedSnapshotChunkCodec.PackPairValues(velX, velY));
        }

        private ShooterPackedComponentChunk ExportProjectileTransformChunk()
        {
            if (_entities.ProjectileCount == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.Transform, ShooterPackedEntityKinds.Projectile);
            }

            var entityIds = ShooterRuntimeSnapshotUtility.CopyAndSort(_entities.ProjectileIds);
            var posX = new float[entityIds.Length];
            var posY = new float[entityIds.Length];
            var velX = new float[entityIds.Length];
            var velY = new float[entityIds.Length];
            var facingX = new float[entityIds.Length];
            var facingY = new float[entityIds.Length];
            for (int i = 0; i < entityIds.Length; i++)
            {
                _entities.TryGetProjectile(entityIds[i], out var bullet);
                posX[i] = bullet.X;
                posY[i] = bullet.Y;
                velX[i] = bullet.VelocityX;
                velY[i] = bullet.VelocityY;
                var dirX = bullet.VelocityX;
                var dirY = bullet.VelocityY;
                ShooterBattleMath.Normalize(ref dirX, ref dirY);
                facingX[i] = dirX;
                facingY[i] = dirY;
            }

            return new ShooterPackedComponentChunk(
                ShooterPackedComponentKinds.Transform,
                ShooterPackedEntityKinds.Projectile,
                entityIds.Length,
                entityIds,
                posX,
                posY,
                facingX,
                facingY,
                Array.Empty<int>(),
                Array.Empty<byte>(),
                Array.Empty<int>(),
                ShooterPackedSnapshotChunkCodec.PackPairValues(velX, velY));
        }

        private ShooterPackedComponentChunk ExportEnemyTransformChunk()
        {
            var (transforms, _, ids, count) = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>(ShooterSveltoGroups.GameplayTargets);
            if (count == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.Transform, ShooterPackedEntityKinds.Enemy);
            }

            var order = CreateSortedEnemyOrder(ids, count);
            var entityIds = new int[count];
            var posX = new float[count];
            var posY = new float[count];
            var facingX = new float[count];
            var facingY = new float[count];
            for (int i = 0; i < count; i++)
            {
                var sourceIndex = order[i];
                entityIds[i] = (int)ids[sourceIndex];
                posX[i] = transforms[sourceIndex].X;
                posY[i] = transforms[sourceIndex].Y;
                facingX[i] = transforms[sourceIndex].DirectionX;
                facingY[i] = transforms[sourceIndex].DirectionY;
            }

            return new ShooterPackedComponentChunk(
                ShooterPackedComponentKinds.Transform,
                ShooterPackedEntityKinds.Enemy,
                entityIds.Length,
                entityIds,
                posX,
                posY,
                facingX,
                facingY,
                Array.Empty<int>(),
                Array.Empty<byte>(),
                Array.Empty<int>(),
                Array.Empty<int>());
        }

        private ShooterPackedComponentChunk ExportPlayerHealthChunk()
        {
            if (_entities.PlayerCount == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.Health, ShooterPackedEntityKinds.Player);
            }

            var entityIds = ShooterRuntimeSnapshotUtility.CopyAndSort(_entities.PlayerIds);
            var hp = new int[entityIds.Length];
            for (int i = 0; i < entityIds.Length; i++)
            {
                _entities.TryGetPlayer(entityIds[i], out var player);
                hp[i] = player.Hp;
            }

            return new ShooterPackedComponentChunk(
                ShooterPackedComponentKinds.Health,
                ShooterPackedEntityKinds.Player,
                entityIds.Length,
                entityIds,
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                hp,
                Array.Empty<byte>(),
                Array.Empty<int>(),
                Array.Empty<int>());
        }

        private ShooterPackedComponentChunk ExportEnemyHealthChunk()
        {
            var (_, healths, ids, count) = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>(ShooterSveltoGroups.GameplayTargets);
            if (count == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.Health, ShooterPackedEntityKinds.Enemy);
            }

            var order = CreateSortedEnemyOrder(ids, count);
            var entityIds = new int[count];
            var hp = new int[count];
            for (int i = 0; i < count; i++)
            {
                var sourceIndex = order[i];
                entityIds[i] = (int)ids[sourceIndex];
                hp[i] = healths[sourceIndex].Current;
            }

            return new ShooterPackedComponentChunk(
                ShooterPackedComponentKinds.Health,
                ShooterPackedEntityKinds.Enemy,
                entityIds.Length,
                entityIds,
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                hp,
                Array.Empty<byte>(),
                Array.Empty<int>(),
                Array.Empty<int>());
        }

        private ShooterPackedComponentChunk ExportPlayerScoreChunk()
        {
            if (_entities.PlayerCount == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.Score, ShooterPackedEntityKinds.Player);
            }

            var entityIds = ShooterRuntimeSnapshotUtility.CopyAndSort(_entities.PlayerIds);
            var scores = new int[entityIds.Length];
            for (int i = 0; i < entityIds.Length; i++)
            {
                _entities.TryGetPlayer(entityIds[i], out var player);
                scores[i] = player.Score;
            }

            return new ShooterPackedComponentChunk(
                ShooterPackedComponentKinds.Score,
                ShooterPackedEntityKinds.Player,
                entityIds.Length,
                entityIds,
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                scores,
                Array.Empty<byte>(),
                Array.Empty<int>(),
                Array.Empty<int>());
        }

        private ShooterPackedComponentChunk ExportProjectileLifetimeChunk()
        {
            if (_entities.ProjectileCount == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.ProjectileLifetime, ShooterPackedEntityKinds.Projectile);
            }

            var entityIds = ShooterRuntimeSnapshotUtility.CopyAndSort(_entities.ProjectileIds);
            var remainingFrames = new int[entityIds.Length];
            for (int i = 0; i < entityIds.Length; i++)
            {
                _entities.TryGetProjectile(entityIds[i], out var bullet);
                remainingFrames[i] = bullet.RemainingFrames;
            }

            return new ShooterPackedComponentChunk(
                ShooterPackedComponentKinds.ProjectileLifetime,
                ShooterPackedEntityKinds.Projectile,
                entityIds.Length,
                entityIds,
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                remainingFrames,
                Array.Empty<byte>(),
                Array.Empty<int>(),
                Array.Empty<int>());
        }

        private int CountEnemies()
        {
            return _context.EntitiesDB.Count<ShooterSveltoHealthComponent>(ShooterSveltoGroups.GameplayTargets);
        }

        private static int[] CreateSortedEnemyOrder(NativeEntityIDs ids, int count)
        {
            var order = new int[count];
            for (int i = 0; i < count; i++)
            {
                order[i] = i;
            }

            Array.Sort(order, (left, right) => ids[left].CompareTo(ids[right]));
            return order;
        }

        private static uint CreateSnapshotFlags(bool isFullSnapshot, bool authorityOverride)
        {
            var flags = isFullSnapshot ? ShooterPackedSnapshotFlags.Full : ShooterPackedSnapshotFlags.Delta;
            if (isFullSnapshot)
            {
                flags |= ShooterPackedSnapshotFlags.KeyFrame;
            }

            if (authorityOverride)
            {
                flags |= ShooterPackedSnapshotFlags.AuthorityOverride;
            }

            return flags;
        }
    }
}
