#nullable enable

using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Shooter.View
{
    public sealed class ShooterViewEntityStore
    {
        private readonly Dictionary<ShooterViewEntityKey, ShooterViewEntityState> _entities = new Dictionary<ShooterViewEntityKey, ShooterViewEntityState>();
        private readonly Dictionary<ShooterViewEntityKey, ShooterViewTransformState> _transforms = new Dictionary<ShooterViewEntityKey, ShooterViewTransformState>();
        private readonly Dictionary<ShooterViewEntityKey, ShooterViewHealthState> _health = new Dictionary<ShooterViewEntityKey, ShooterViewHealthState>();
        private readonly Dictionary<ShooterViewEntityKey, ShooterViewScoreState> _scores = new Dictionary<ShooterViewEntityKey, ShooterViewScoreState>();
        private readonly Dictionary<ShooterViewEntityKey, ShooterViewProjectileLifetimeState> _projectileLifetimes = new Dictionary<ShooterViewEntityKey, ShooterViewProjectileLifetimeState>();

        public IReadOnlyDictionary<ShooterViewEntityKey, ShooterViewEntityState> Entities => _entities;

        public IReadOnlyDictionary<ShooterViewEntityKey, ShooterViewTransformState> Transforms => _transforms;

        public IReadOnlyDictionary<ShooterViewEntityKey, ShooterViewHealthState> Health => _health;

        public IReadOnlyDictionary<ShooterViewEntityKey, ShooterViewScoreState> Scores => _scores;

        public IReadOnlyDictionary<ShooterViewEntityKey, ShooterViewProjectileLifetimeState> ProjectileLifetimes => _projectileLifetimes;

        public int EntityCount => _entities.Count;

        public int PlayerCount => CountEntities(ShooterViewEntityKind.Player);

        public int BulletCount => CountEntities(ShooterViewEntityKind.Bullet);

        public void Clear()
        {
            _entities.Clear();
            _transforms.Clear();
            _health.Clear();
            _scores.Clear();
            _projectileLifetimes.Clear();
        }

        public void UpsertEntity(in ShooterViewEntityChange change)
        {
            if (!change.Alive)
            {
                RemoveEntity(change.Key);
                return;
            }

            _entities[change.Key] = new ShooterViewEntityState(change.Key, change.OwnerEntityId, change.Alive);
        }

        public bool RemoveEntity(ShooterViewEntityKey key)
        {
            var removed = _entities.Remove(key);
            _transforms.Remove(key);
            _health.Remove(key);
            _scores.Remove(key);
            _projectileLifetimes.Remove(key);
            return removed;
        }

        public void UpsertTransform(in ShooterViewTransformComponentChange change)
        {
            if (!_entities.ContainsKey(change.Key))
            {
                return;
            }

            _transforms[change.Key] = new ShooterViewTransformState(
                change.Key,
                change.X,
                change.Y,
                change.FacingX,
                change.FacingY,
                change.VelocityX,
                change.VelocityY);
        }

        public void UpsertHealth(in ShooterViewHealthComponentChange change)
        {
            if (!_entities.ContainsKey(change.Key))
            {
                return;
            }

            _health[change.Key] = new ShooterViewHealthState(change.Key, change.Hp);
        }

        public void UpsertScore(in ShooterViewScoreComponentChange change)
        {
            if (!_entities.ContainsKey(change.Key))
            {
                return;
            }

            _scores[change.Key] = new ShooterViewScoreState(change.Key, change.Score);
        }

        public void UpsertProjectileLifetime(in ShooterViewProjectileLifetimeComponentChange change)
        {
            if (!_entities.ContainsKey(change.Key))
            {
                return;
            }

            _projectileLifetimes[change.Key] = new ShooterViewProjectileLifetimeState(change.Key, change.RemainingFrames);
        }

        public bool ContainsEntity(ShooterViewEntityKey key)
        {
            return _entities.ContainsKey(key);
        }

        public bool TryGetEntity(ShooterViewEntityKey key, out ShooterViewEntityState state)
        {
            return _entities.TryGetValue(key, out state);
        }

        public bool TryGetTransform(ShooterViewEntityKey key, out ShooterViewTransformState state)
        {
            return _transforms.TryGetValue(key, out state);
        }

        public bool TryGetHealth(ShooterViewEntityKey key, out ShooterViewHealthState state)
        {
            return _health.TryGetValue(key, out state);
        }

        public bool TryGetScore(ShooterViewEntityKey key, out ShooterViewScoreState state)
        {
            return _scores.TryGetValue(key, out state);
        }

        public bool TryGetProjectileLifetime(ShooterViewEntityKey key, out ShooterViewProjectileLifetimeState state)
        {
            return _projectileLifetimes.TryGetValue(key, out state);
        }

        private int CountEntities(ShooterViewEntityKind kind)
        {
            var count = 0;
            foreach (var entity in _entities.Values)
            {
                if (entity.Kind == kind && entity.Alive)
                {
                    count++;
                }
            }

            return count;
        }
    }

    public readonly struct ShooterViewEntityState
    {
        public ShooterViewEntityState(ShooterViewEntityKey key, int ownerEntityId, bool alive)
        {
            Key = key;
            OwnerEntityId = ownerEntityId;
            Alive = alive;
        }

        public ShooterViewEntityKey Key { get; }

        public ShooterViewEntityKind Kind => Key.Kind;

        public int EntityId => Key.EntityId;

        public int OwnerEntityId { get; }

        public bool Alive { get; }
    }

    public readonly struct ShooterViewTransformState
    {
        public ShooterViewTransformState(
            ShooterViewEntityKey key,
            float x,
            float y,
            float facingX,
            float facingY,
            float velocityX,
            float velocityY)
        {
            Key = key;
            X = x;
            Y = y;
            FacingX = facingX;
            FacingY = facingY;
            VelocityX = velocityX;
            VelocityY = velocityY;
        }

        public ShooterViewEntityKey Key { get; }

        public float X { get; }

        public float Y { get; }

        public float FacingX { get; }

        public float FacingY { get; }

        public float VelocityX { get; }

        public float VelocityY { get; }
    }

    public readonly struct ShooterViewHealthState
    {
        public ShooterViewHealthState(ShooterViewEntityKey key, int hp)
        {
            Key = key;
            Hp = hp;
        }

        public ShooterViewEntityKey Key { get; }

        public int Hp { get; }
    }

    public readonly struct ShooterViewScoreState
    {
        public ShooterViewScoreState(ShooterViewEntityKey key, int score)
        {
            Key = key;
            Score = score;
        }

        public ShooterViewEntityKey Key { get; }

        public int Score { get; }
    }

    public readonly struct ShooterViewProjectileLifetimeState
    {
        public ShooterViewProjectileLifetimeState(ShooterViewEntityKey key, int remainingFrames)
        {
            Key = key;
            RemainingFrames = remainingFrames;
        }

        public ShooterViewEntityKey Key { get; }

        public int RemainingFrames { get; }
    }
}
