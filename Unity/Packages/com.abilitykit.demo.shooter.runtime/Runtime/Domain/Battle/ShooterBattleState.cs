using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.Runtime
{
    [WorldService(typeof(ShooterBattleState), WorldLifetime.Singleton)]
    public sealed class ShooterBattleState
    {
        private readonly IShooterEntityManager _entities;
        private int _nextBulletId = 1;

        public ShooterBattleState(IShooterEntityManager entities)
        {
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
        }

        public IShooterEntityManager Entities => _entities;

        public Dictionary<int, ShooterPlayerCommand> LatestCommands { get; } = new Dictionary<int, ShooterPlayerCommand>();

        public List<ShooterEventSnapshot> Events { get; } = new List<ShooterEventSnapshot>(16);

        public bool IsStarted { get; set; }

        public int CurrentFrame { get; set; }

        public ShooterStartGamePayload StartSpec { get; set; }

        public void Reset(in ShooterStartGamePayload spec)
        {
            _entities.Clear();
            LatestCommands.Clear();
            Events.Clear();
            _nextBulletId = 1;
            CurrentFrame = 0;
            StartSpec = spec;
            IsStarted = false;
        }

        public int AllocateBulletId()
        {
            return _nextBulletId++;
        }

        public void AdvanceBulletIdPast(int bulletId)
        {
            if (bulletId >= _nextBulletId)
            {
                _nextBulletId = bulletId + 1;
            }
        }
    }
}
