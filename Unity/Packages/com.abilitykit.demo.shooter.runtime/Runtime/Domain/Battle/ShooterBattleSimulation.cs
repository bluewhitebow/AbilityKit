#nullable enable

using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Protocol.Shooter;
using AbilityKit.World.Svelto;
using Svelto.ECS;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public interface IShooterBattleSimulation
    {
        void Tick(float deltaTime);
    }

    [WorldService(typeof(ShooterBattleSimulation), WorldLifetime.Singleton)]
    [WorldService(typeof(IShooterBattleSimulation), WorldLifetime.Singleton)]
    public sealed class ShooterBattleSimulation : IShooterBattleSimulation
    {
        private readonly ShooterBattleState _state;
        private readonly IShooterEntityManager _entities;
        private readonly IShooterBattleRules _rules;
        private readonly List<int> _playerIdBuffer = new List<int>(8);
        private readonly List<int> _projectileIdBuffer = new List<int>(32);
        private readonly ISveltoWorldContext _context;

        public ShooterBattleSimulation(ShooterBattleState state)
            : this(state, ShooterBattleRules.Default)
        {
        }

        public ShooterBattleSimulation(ShooterBattleState state, IShooterBattleRules rules)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _entities = _state.Entities;
            _context = _entities.SveltoContext;
        }

        public void Tick(float deltaTime)
        {
            TickPlayers(deltaTime);
            TickBullets(deltaTime);
        }

        private void TickPlayers(float deltaTime)
        {
            CopyIds(_entities.PlayerIds, _playerIdBuffer);
            for (int i = 0; i < _playerIdBuffer.Count; i++)
            {
                if (!_entities.TryGetPlayer(_playerIdBuffer[i], out var player) || !player.Alive)
                {
                    continue;
                }

                _state.LatestCommands.TryGetValue(player.PlayerId, out var command);
                var moveLength = ShooterBattleMath.Normalize(ref command.MoveX, ref command.MoveY);
                if (moveLength > 0f)
                {
                    player.X += command.MoveX * _rules.PlayerSpeed * deltaTime;
                    player.Y += command.MoveY * _rules.PlayerSpeed * deltaTime;
                }

                var aimLength = ShooterBattleMath.Normalize(ref command.AimX, ref command.AimY);
                if (aimLength > 0f)
                {
                    player.AimX = command.AimX;
                    player.AimY = command.AimY;
                }

                if (command.Fire)
                {
                    SpawnBullet(in player);
                    command.Fire = false;
                    _state.LatestCommands[player.PlayerId] = command;
                }

                _entities.SetPlayer(in player);
            }
        }

        private void TickBullets(float deltaTime)
        {
            CopyIds(_entities.ProjectileIds, _projectileIdBuffer);
            for (int i = _projectileIdBuffer.Count - 1; i >= 0; i--)
            {
                var bulletId = _projectileIdBuffer[i];
                if (!_entities.TryGetProjectile(bulletId, out var bullet))
                {
                    continue;
                }

                bullet.X += bullet.VelocityX * deltaTime;
                bullet.Y += bullet.VelocityY * deltaTime;
                bullet.RemainingFrames--;

                if (TryHitPlayer(in bullet, out var target))
                {
                    target.Hp = Math.Max(0, target.Hp - _rules.HitDamage);
                    if (target.Hp == 0)
                    {
                        target.Alive = false;
                    }

                    _entities.SetPlayer(in target);
                    if (_entities.TryGetPlayer(bullet.OwnerPlayerId, out var owner))
                    {
                        owner.Score++;
                        _entities.SetPlayer(in owner);
                    }

                    _state.Events.Add(new ShooterEventSnapshot(ShooterEventType.Hit, bullet.OwnerPlayerId, target.PlayerId, bullet.BulletId, target.X, target.Y, _rules.HitDamage));
                    _entities.RemoveProjectile(bullet.BulletId);
                    continue;
                }

                if (TryHitEnemy(in bullet, out var enemyId, out var enemyX, out var enemyY, out var defeated))
                {
                    if (defeated && _entities.TryGetPlayer(bullet.OwnerPlayerId, out var owner))
                    {
                        owner.Score++;
                        _entities.SetPlayer(in owner);
                    }

                    _state.Events.Add(new ShooterEventSnapshot(ShooterEventType.Hit, bullet.OwnerPlayerId, -(int)enemyId, bullet.BulletId, enemyX, enemyY, _rules.HitDamage));
                    _entities.RemoveProjectile(bullet.BulletId);
                    continue;
                }

                if (bullet.RemainingFrames <= 0)
                {
                    _entities.RemoveProjectile(bullet.BulletId);
                    continue;
                }

                _entities.SetProjectile(in bullet);
            }
        }

        private void SpawnBullet(in ShooterSveltoPlayerComponent player)
        {
            var bullet = new ShooterSveltoProjectileComponent
            {
                BulletId = _state.AllocateBulletId(),
                OwnerPlayerId = player.PlayerId,
                X = player.X + player.AimX * 0.5f,
                Y = player.Y + player.AimY * 0.5f,
                VelocityX = player.AimX * _rules.BulletSpeed,
                VelocityY = player.AimY * _rules.BulletSpeed,
                RemainingFrames = _rules.BulletLifeFrames
            };

            _entities.AddProjectile(in bullet);
            _state.Events.Add(new ShooterEventSnapshot(ShooterEventType.Fire, player.PlayerId, 0, bullet.BulletId, bullet.X, bullet.Y, 0));
        }

        private bool TryHitPlayer(in ShooterSveltoProjectileComponent bullet, out ShooterSveltoPlayerComponent target)
        {
            CopyIds(_entities.PlayerIds, _playerIdBuffer);
            for (int i = 0; i < _playerIdBuffer.Count; i++)
            {
                if (!_entities.TryGetPlayer(_playerIdBuffer[i], out var player) || !player.Alive || player.PlayerId == bullet.OwnerPlayerId)
                {
                    continue;
                }

                var dx = player.X - bullet.X;
                var dy = player.Y - bullet.Y;
                if (dx * dx + dy * dy <= _rules.HitRadius * _rules.HitRadius)
                {
                    target = player;
                    return true;
                }
            }

            target = default;
            return false;
        }

        private bool TryHitEnemy(in ShooterSveltoProjectileComponent bullet, out uint enemyId, out float enemyX, out float enemyY, out bool defeated)
        {
            var (transforms, healths, ids, count) = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>(ShooterSveltoGroups.GameplayTargets);
            for (int i = 0; i < count; i++)
            {
                if (healths[i].Alive == 0)
                {
                    continue;
                }

                var dx = transforms[i].X - bullet.X;
                var dy = transforms[i].Y - bullet.Y;
                if (dx * dx + dy * dy > _rules.HitRadius * _rules.HitRadius)
                {
                    continue;
                }

                healths[i].Current = Math.Max(0, healths[i].Current - _rules.HitDamage);
                defeated = healths[i].Current == 0;
                if (defeated)
                {
                    healths[i].Alive = 0;
                }

                enemyId = ids[i];
                enemyX = transforms[i].X;
                enemyY = transforms[i].Y;
                return true;
            }

            enemyId = 0u;
            enemyX = 0f;
            enemyY = 0f;
            defeated = false;
            return false;
        }

        private static void CopyIds(IReadOnlyCollection<int> source, List<int> destination)
        {
            destination.Clear();
            foreach (var id in source)
            {
                destination.Add(id);
            }
        }
    }
}
