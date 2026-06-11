using System;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.DI;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public sealed class ShooterLogicWorld : IWorld
    {
        private readonly WorldContainer _container;

        public ShooterLogicWorld(WorldCreateOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            Id = options.Id;
            WorldType = string.IsNullOrEmpty(options.WorldType) ? ShooterGameplay.WorldType : options.WorldType;

            var builder = options.ServiceBuilder ?? new WorldContainerBuilder();
            for (int i = 0; i < options.Modules.Count; i++)
            {
                if (options.Modules[i] != null)
                {
                    builder.AddModule(options.Modules[i]);
                }
            }

            _container = builder.Build();
        }

        public WorldId Id { get; }

        public string WorldType { get; }

        public IWorldResolver Services => _container;

        public void Initialize()
        {
        }

        public void Tick(float deltaTime)
        {
            if (_container.TryResolve<IShooterBattleRuntimePort>(out var runtime) && runtime.IsStarted)
            {
                runtime.Tick(deltaTime);
            }
        }

        public void Dispose()
        {
            _container.Dispose();
        }
    }
}
