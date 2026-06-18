#nullable enable

using System;
using AbilityKit.Protocol.Shooter;
using AbilityKit.World.Svelto;
using Svelto.ECS;

namespace AbilityKit.Demo.Shooter.Runtime
{
    internal sealed class ShooterEnemyWaveBattleSystem : IShooterBattleSystem
    {
        private const float Pi = 3.14159265358979323846f;
        private const int FirstEnemyEntityId = 10000;
        private readonly ShooterBattleState _state;
        private readonly IShooterEntityManager _entities;
        private readonly ISveltoWorldContext _context;
        private readonly int[] _waveSpawned = new int[3];
        private int _nextEnemyId = FirstEnemyEntityId;

        public ShooterEnemyWaveBattleSystem(IShooterBattleServiceResolver services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            _state = services.Resolve<ShooterBattleState>();
            _entities = services.Resolve<IShooterEntityManager>();
            _context = services.Resolve<ISveltoWorldContext>();
        }

        public int Order => ShooterBattleSystemOrder.EnemyWave;

        public string name => nameof(ShooterEnemyWaveBattleSystem);

        public void Step(in float deltaTime)
        {
            if (_state.CurrentFrame <= 1)
            {
                ResetWaveState();
            }

            TickWaveSpawns();
            TickEnemyAttacks();
        }

        private void ResetWaveState()
        {
            if (_context.EntitiesDB.ExistsAndIsNotEmpty(ShooterSveltoGroups.GameplayTargets))
            {
                _context.EntityFunctions.RemoveEntitiesFromGroup(ShooterSveltoGroups.GameplayTargets);
                _context.SubmitEntities();
            }

            Array.Clear(_waveSpawned, 0, _waveSpawned.Length);
            _nextEnemyId = FirstEnemyEntityId;
        }

        private void TickWaveSpawns()
        {
            var activeEnemies = CountAliveEnemies();
            TickWave(index: 0, waveId: 1, startFrame: 1, spawnFrameInterval: 2, enemyCount: 24, enemyHp: 2, spawnRadius: 7f, ref activeEnemies);
            TickWave(index: 1, waveId: 2, startFrame: 30, spawnFrameInterval: 2, enemyCount: 24, enemyHp: 2, spawnRadius: 9f, ref activeEnemies);
            TickWave(index: 2, waveId: 3, startFrame: 60, spawnFrameInterval: 3, enemyCount: 24, enemyHp: 3, spawnRadius: 11f, ref activeEnemies);
        }

        private void TickWave(int index, int waveId, int startFrame, int spawnFrameInterval, int enemyCount, int enemyHp, float spawnRadius, ref int activeEnemies)
        {
            const int maxActiveEnemies = 36;
            if (_state.CurrentFrame < startFrame || _waveSpawned[index] >= enemyCount || activeEnemies >= maxActiveEnemies)
            {
                return;
            }

            var framesSinceStart = _state.CurrentFrame - startFrame;
            if (framesSinceStart % spawnFrameInterval != 0)
            {
                return;
            }

            SpawnEnemy(waveId, _waveSpawned[index], enemyHp, spawnRadius);
            _waveSpawned[index]++;
            activeEnemies++;
        }

        private void SpawnEnemy(int waveId, int spawnIndex, int enemyHp, float spawnRadius)
        {
            var enemyId = (uint)_nextEnemyId++;
            var angle = (waveId * 97 + spawnIndex * 37) * Pi / 180f;
            var x = MathF.Cos(angle) * spawnRadius;
            var y = MathF.Sin(angle) * spawnRadius;
            var directionX = -x;
            var directionY = -y;
            Normalize(ref directionX, ref directionY);

            var initializer = _context.EntityFactory.BuildEntity<ShooterSveltoGameplayTargetDescriptor>(enemyId, ShooterSveltoGroups.GameplayTargets);
            initializer.Init(new ShooterSveltoTransformComponent
            {
                X = x,
                Y = y,
                DirectionX = directionX,
                DirectionY = directionY
            });
            initializer.Init(new ShooterSveltoHealthComponent
            {
                Current = enemyHp,
                Max = enemyHp,
                Alive = 1
            });
            _context.SubmitEntities();
        }

        private void TickEnemyAttacks()
        {
            if (_state.CurrentFrame % 12 != 0 || _entities.PlayerCount == 0)
            {
                return;
            }

            var (transforms, healths, ids, count) = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>(ShooterSveltoGroups.GameplayTargets);
            for (var i = 0; i < count; i++)
            {
                if (healths[i].Alive == 0)
                {
                    continue;
                }

                if (!TryFindNearestAlivePlayer(in transforms[i], out var player))
                {
                    continue;
                }

                player.Hp = Math.Max(0, player.Hp - 1);
                if (player.Hp == 0)
                {
                    player.Alive = false;
                }

                _entities.SetPlayer(in player);
                _state.Events.Add(new ShooterEventSnapshot(ShooterEventType.Hit, -(int)ids[i], player.PlayerId, 0, transforms[i].X, transforms[i].Y, 1));
            }
        }

        private bool TryFindNearestAlivePlayer(in ShooterSveltoTransformComponent enemy, out ShooterSveltoPlayerComponent nearestPlayer)
        {
            nearestPlayer = default;
            var bestDistance = float.MaxValue;
            foreach (var playerId in _entities.PlayerIds)
            {
                if (!_entities.TryGetPlayer(playerId, out var player) || !player.Alive)
                {
                    continue;
                }

                var dx = player.X - enemy.X;
                var dy = player.Y - enemy.Y;
                var distance = dx * dx + dy * dy;
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                nearestPlayer = player;
            }

            return bestDistance < float.MaxValue;
        }

        private int CountAliveEnemies()
        {
            var alive = 0;
            var (healths, count) = _context.EntitiesDB.QueryEntities<ShooterSveltoHealthComponent>(ShooterSveltoGroups.GameplayTargets);
            for (var i = 0; i < count; i++)
            {
                if (healths[i].Alive != 0)
                {
                    alive++;
                }
            }

            return alive;
        }

        private static void Normalize(ref float x, ref float y)
        {
            var lengthSquared = x * x + y * y;
            if (lengthSquared <= 0.000001f)
            {
                x = 1f;
                y = 0f;
                return;
            }

            var inv = 1f / MathF.Sqrt(lengthSquared);
            x *= inv;
            y *= inv;
        }
    }
}
