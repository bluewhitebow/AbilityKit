using System;
using System.Reflection;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public sealed class ShooterServicesAutoModule : IWorldModule
    {
        private readonly Assembly _targetAssembly;

        public static readonly string[] TargetNamespacePrefixes =
        {
            "AbilityKit.Demo.Shooter.Runtime"
        };

        public ShooterServicesAutoModule()
            : this(typeof(ShooterServicesAutoModule).Assembly)
        {
        }

        public ShooterServicesAutoModule(Assembly targetAssembly)
        {
            _targetAssembly = targetAssembly ?? typeof(ShooterServicesAutoModule).Assembly;
        }

        public void Configure(WorldContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.AddModule(new AttributeWorldServicesModule(
                WorldServiceProfile.All,
                assemblies: new[] { _targetAssembly },
                namespacePrefixes: TargetNamespacePrefixes));
        }
    }
}
