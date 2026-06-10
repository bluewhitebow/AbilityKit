using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Protocol.Shooter;
using AbilityKit.World.Svelto;

namespace AbilityKit.Demo.Shooter.Runtime
{
    [WorldService(typeof(ShooterBattleRuntimePort), WorldLifetime.Singleton)]
    [WorldService(typeof(IShooterBattleRuntimePort), WorldLifetime.Singleton)]
    public sealed class ShooterBattleRuntimePort : IShooterBattleRuntimePort
    {
        private readonly ShooterBattleState _state;
        private readonly IShooterBattleSimulation _simulation;
        private readonly IShooterEntityManager _entities;

        public ShooterBattleRuntimePort()
            : this(CreateDefaultEntityManager())
        {
        }

        private ShooterBattleRuntimePort(IShooterEntityManager entities)
            : this(CreateState(entities))
        {
        }

        private ShooterBattleRuntimePort(ShooterBattleState state)
            : this(state, new ShooterBattleSimulation(state), state.Entities)
        {
        }

        public ShooterBattleRuntimePort(ShooterBattleState state, IShooterBattleSimulation simulation, IShooterEntityManager entities)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
        }

        public bool IsStarted => _state.IsStarted;

        public int CurrentFrame => _state.CurrentFrame;

        public ShooterStartGamePayload StartSpec => _state.StartSpec;

        public bool StartGame(in ShooterStartGamePayload spec)
        {
            _state.Reset(in spec);

            var players = spec.Players ?? Array.Empty<ShooterStartPlayer>();
            for (int i = 0; i < players.Length; i++)
            {
                var player = players[i];
                if (player.PlayerId <= 0 || _entities.HasPlayer(player.PlayerId)) continue;

                var component = new ShooterSveltoPlayerComponent
                {
                    PlayerId = player.PlayerId,
                    X = player.SpawnX,
                    Y = player.SpawnY,
                    AimX = 1f,
                    AimY = 0f,
                    Hp = ShooterGameplay.DefaultPlayerHp,
                    Score = 0,
                    Alive = true
                };
                _entities.AddPlayer(in component);
            }

            _state.IsStarted = _entities.PlayerCount > 0;
            return _state.IsStarted;
        }

        public int SubmitInput(int frame, ShooterPlayerCommand[] commands)
        {
            if (!_state.IsStarted || commands == null || commands.Length == 0)
            {
                return 0;
            }

            var accepted = 0;
            for (int i = 0; i < commands.Length; i++)
            {
                var command = commands[i];
                if (!_entities.HasPlayer(command.PlayerId)) continue;

                _state.LatestCommands[command.PlayerId] = command;
                accepted++;
            }

            return accepted;
        }

        public bool Tick(float deltaTime)
        {
            if (!_state.IsStarted)
            {
                return false;
            }

            _state.CurrentFrame++;
            _state.Events.Clear();

            _simulation.Tick(deltaTime);
            return true;
        }

        public ShooterStateSnapshotPayload GetSnapshot()
        {
            var playerIds = CopyAndSort(_entities.PlayerIds);
            var players = new ShooterPlayerSnapshot[playerIds.Length];
            for (int i = 0; i < playerIds.Length; i++)
            {
                _entities.TryGetPlayer(playerIds[i], out var p);
                players[i] = new ShooterPlayerSnapshot(p.PlayerId, p.X, p.Y, p.AimX, p.AimY, p.Hp, p.Score, p.Alive);
            }

            var bulletIds = CopyAndSort(_entities.ProjectileIds);
            var bullets = new ShooterBulletSnapshot[bulletIds.Length];
            for (int i = 0; i < bulletIds.Length; i++)
            {
                _entities.TryGetProjectile(bulletIds[i], out var b);
                bullets[i] = new ShooterBulletSnapshot(b.BulletId, b.OwnerPlayerId, b.X, b.Y, b.VelocityX, b.VelocityY, b.RemainingFrames);
            }

            return new ShooterStateSnapshotPayload(CurrentFrame, players, bullets, _state.Events.ToArray());
        }

        public uint ComputeStateHash()
        {
            unchecked
            {
                var hash = 2166136261u;
                hash = Hash(hash, CurrentFrame);

                var playerIds = CopyAndSort(_entities.PlayerIds);
                for (int i = 0; i < playerIds.Length; i++)
                {
                    _entities.TryGetPlayer(playerIds[i], out var player);
                    hash = Hash(hash, player.PlayerId);
                    hash = Hash(hash, Quantize(player.X));
                    hash = Hash(hash, Quantize(player.Y));
                    hash = Hash(hash, Quantize(player.AimX));
                    hash = Hash(hash, Quantize(player.AimY));
                    hash = Hash(hash, player.Hp);
                    hash = Hash(hash, player.Score);
                    hash = Hash(hash, player.Alive ? 1 : 0);
                }

                var bulletIds = CopyAndSort(_entities.ProjectileIds);
                for (int i = 0; i < bulletIds.Length; i++)
                {
                    _entities.TryGetProjectile(bulletIds[i], out var bullet);
                    hash = Hash(hash, bullet.BulletId);
                    hash = Hash(hash, bullet.OwnerPlayerId);
                    hash = Hash(hash, Quantize(bullet.X));
                    hash = Hash(hash, Quantize(bullet.Y));
                    hash = Hash(hash, Quantize(bullet.VelocityX));
                    hash = Hash(hash, Quantize(bullet.VelocityY));
                    hash = Hash(hash, bullet.RemainingFrames);
                }

                return hash;
            }
        }

        public ShooterPackedSnapshotPayload ExportPackedSnapshot(ulong worldId, bool isFullSnapshot = true, bool authorityOverride = false)
        {
            var chunks = new[]
            {
                ExportPlayerChunk(),
                ExportProjectileChunk()
            };

            return new ShooterPackedSnapshotPayload(
                ShooterPackedSnapshotCodec.CurrentVersion,
                worldId,
                CurrentFrame,
                CurrentFrame,
                CreateSnapshotFlags(isFullSnapshot, authorityOverride),
                ComputeStateHash(),
                _entities.PlayerCount + _entities.ProjectileCount,
                chunks,
                Array.Empty<byte>());
        }

        public bool ImportPackedSnapshot(in ShooterPackedSnapshotPayload snapshot)
        {
            if (snapshot.Version <= 0 || snapshot.Chunks == null)
            {
                return false;
            }

            _state.Reset(default);
            _state.CurrentFrame = snapshot.Frame;

            for (int i = 0; i < snapshot.Chunks.Length; i++)
            {
                var chunk = snapshot.Chunks[i];
                switch (chunk.EntityKind)
                {
                    case ShooterPackedEntityKinds.Player:
                        ImportPlayerChunk(in chunk);
                        break;
                    case ShooterPackedEntityKinds.Projectile:
                        ImportProjectileChunk(in chunk);
                        break;
                }
            }

            _state.IsStarted = _entities.PlayerCount > 0;
            return _state.IsStarted || snapshot.EntityCount == 0;
        }

        public byte[] ExportPackedSnapshotBytes(ulong worldId, bool isFullSnapshot = true, bool authorityOverride = false)
        {
            var snapshot = ExportPackedSnapshot(worldId, isFullSnapshot, authorityOverride);
            return ShooterPackedSnapshotCodec.Serialize(in snapshot);
        }

        public bool ImportPackedSnapshotBytes(byte[] payload)
        {
            var snapshot = ShooterPackedSnapshotCodec.Deserialize(payload);
            return ImportPackedSnapshot(in snapshot);
        }

        private ShooterPackedEntityChunk ExportPlayerChunk()
        {
            if (_entities.PlayerCount == 0)
            {
                return ShooterPackedEntityChunk.Empty(ShooterPackedEntityKinds.Player);
            }

            var entityIds = CopyAndSort(_entities.PlayerIds);
            var posX = new float[entityIds.Length];
            var posY = new float[entityIds.Length];
            var velX = new float[entityIds.Length];
            var velY = new float[entityIds.Length];
            var facingX = new float[entityIds.Length];
            var facingY = new float[entityIds.Length];
            var hp = new short[entityIds.Length];
            var flags = new byte[entityIds.Length];
            var ownerIds = new int[entityIds.Length];
            var aux = new int[entityIds.Length];

            for (int i = 0; i < entityIds.Length; i++)
            {
                _entities.TryGetPlayer(entityIds[i], out var player);
                posX[i] = player.X;
                posY[i] = player.Y;
                facingX[i] = player.AimX;
                facingY[i] = player.AimY;
                hp[i] = ClampToShort(player.Hp);
                flags[i] = (byte)(ShooterPackedEntityFlags.Player | ShooterPackedEntityFlags.DirtyTransform | ShooterPackedEntityFlags.DirtyStats);
                if (player.Alive)
                {
                    flags[i] |= ShooterPackedEntityFlags.Alive;
                }

                if (_state.LatestCommands.TryGetValue(player.PlayerId, out var command))
                {
                    var moveX = command.MoveX;
                    var moveY = command.MoveY;
                    if (Normalize(ref moveX, ref moveY) > 0f)
                    {
                        velX[i] = moveX * ShooterBattleTuning.PlayerSpeed;
                        velY[i] = moveY * ShooterBattleTuning.PlayerSpeed;
                    }
                }

                ownerIds[i] = player.PlayerId;
                aux[i] = player.Score;
            }

            return new ShooterPackedEntityChunk(ShooterPackedEntityKinds.Player, entityIds.Length, entityIds, posX, posY, velX, velY, facingX, facingY, hp, flags, ownerIds, aux);
        }

        private ShooterPackedEntityChunk ExportProjectileChunk()
        {
            if (_entities.ProjectileCount == 0)
            {
                return ShooterPackedEntityChunk.Empty(ShooterPackedEntityKinds.Projectile);
            }

            var entityIds = CopyAndSort(_entities.ProjectileIds);
            var posX = new float[entityIds.Length];
            var posY = new float[entityIds.Length];
            var velX = new float[entityIds.Length];
            var velY = new float[entityIds.Length];
            var facingX = new float[entityIds.Length];
            var facingY = new float[entityIds.Length];
            var hp = new short[entityIds.Length];
            var flags = new byte[entityIds.Length];
            var ownerIds = new int[entityIds.Length];
            var aux = new int[entityIds.Length];

            for (int i = 0; i < entityIds.Length; i++)
            {
                _entities.TryGetProjectile(entityIds[i], out var bullet);
                posX[i] = bullet.X;
                posY[i] = bullet.Y;
                velX[i] = bullet.VelocityX;
                velY[i] = bullet.VelocityY;
                var dirX = bullet.VelocityX;
                var dirY = bullet.VelocityY;
                Normalize(ref dirX, ref dirY);
                facingX[i] = dirX;
                facingY[i] = dirY;
                hp[i] = ClampToShort(bullet.RemainingFrames);
                flags[i] = (byte)(ShooterPackedEntityFlags.Alive | ShooterPackedEntityFlags.Projectile | ShooterPackedEntityFlags.DirtyTransform);
                ownerIds[i] = bullet.OwnerPlayerId;
                aux[i] = bullet.RemainingFrames;
            }

            return new ShooterPackedEntityChunk(ShooterPackedEntityKinds.Projectile, entityIds.Length, entityIds, posX, posY, velX, velY, facingX, facingY, hp, flags, ownerIds, aux);
        }

        private void ImportPlayerChunk(in ShooterPackedEntityChunk chunk)
        {
            var count = Math.Max(0, chunk.Count);
            for (int i = 0; i < count; i++)
            {
                var playerId = GetInt(chunk.EntityIds, i);
                if (playerId <= 0) continue;

                var flags = GetByte(chunk.Flags, i);
                var player = new ShooterSveltoPlayerComponent
                {
                    PlayerId = playerId,
                    X = GetFloat(chunk.PosX, i),
                    Y = GetFloat(chunk.PosY, i),
                    AimX = GetFloat(chunk.FacingX, i, 1f),
                    AimY = GetFloat(chunk.FacingY, i),
                    Hp = GetShort(chunk.Hp, i),
                    Score = GetInt(chunk.Aux, i),
                    Alive = (flags & ShooterPackedEntityFlags.Alive) != 0
                };
                _entities.AddPlayer(in player);
            }
        }

        private void ImportProjectileChunk(in ShooterPackedEntityChunk chunk)
        {
            var count = Math.Max(0, chunk.Count);
            for (int i = 0; i < count; i++)
            {
                var bulletId = GetInt(chunk.EntityIds, i);
                if (bulletId <= 0) continue;

                var remainingFrames = GetInt(chunk.Aux, i, GetShort(chunk.Hp, i));
                var bullet = new ShooterSveltoProjectileComponent
                {
                    BulletId = bulletId,
                    OwnerPlayerId = GetInt(chunk.OwnerIds, i),
                    X = GetFloat(chunk.PosX, i),
                    Y = GetFloat(chunk.PosY, i),
                    VelocityX = GetFloat(chunk.VelX, i),
                    VelocityY = GetFloat(chunk.VelY, i),
                    RemainingFrames = remainingFrames
                };
                _entities.AddProjectile(in bullet);
                _state.AdvanceBulletIdPast(bulletId);
            }
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

        private static short ClampToShort(int value)
        {
            if (value < short.MinValue) return short.MinValue;
            if (value > short.MaxValue) return short.MaxValue;
            return (short)value;
        }

        private static int GetInt(int[] values, int index, int fallback = 0)
        {
            return values != null && index >= 0 && index < values.Length ? values[index] : fallback;
        }

        private static float GetFloat(float[] values, int index, float fallback = 0f)
        {
            return values != null && index >= 0 && index < values.Length ? values[index] : fallback;
        }

        private static int GetShort(short[] values, int index, int fallback = 0)
        {
            return values != null && index >= 0 && index < values.Length ? values[index] : fallback;
        }

        private static byte GetByte(byte[] values, int index, byte fallback = 0)
        {
            return values != null && index >= 0 && index < values.Length ? values[index] : fallback;
        }

        private static int Quantize(float value)
        {
            return (int)Math.Round(value * 10000f);
        }

        private static uint Hash(uint hash, int value)
        {
            unchecked
            {
                hash ^= (uint)value;
                hash *= 16777619u;
                return hash;
            }
        }

        private static float Normalize(ref float x, ref float y)
        {
            return ShooterBattleMath.Normalize(ref x, ref y);
        }

        private static int[] CopyAndSort(IReadOnlyCollection<int> ids)
        {
            var sorted = new int[ids.Count];
            var index = 0;
            foreach (var id in ids)
            {
                sorted[index++] = id;
            }

            Array.Sort(sorted);
            return sorted;
        }

        private static IShooterEntityManager CreateDefaultEntityManager()
        {
            return new ShooterEntityManager(new SveltoWorldContext());
        }

        private static ShooterBattleState CreateState(IShooterEntityManager entities)
        {
            return new ShooterBattleState(entities);
        }
    }
}
