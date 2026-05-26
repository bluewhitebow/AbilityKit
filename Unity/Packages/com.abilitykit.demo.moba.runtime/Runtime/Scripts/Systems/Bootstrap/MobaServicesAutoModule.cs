using System;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;

namespace AbilityKit.Demo.Moba.Systems
{
    /// <summary>
    /// Auto service discovery module for Moba runtime services.
    /// Scans all loaded assemblies for classes marked with [WorldService] attribute
    /// and automatically registers them to the WorldContainerBuilder.
    ///
    /// Usage:
    /// 1. Mark service classes with [WorldService(typeof(TService))]
    /// 2. Add this module to the builder:
    ///    builder.AddModule(new MobaServicesAutoModule());
    ///
    /// Service classes must:
    /// - Implement IService interface
    /// - Have a public constructor (dependencies auto-injected via WorldActivator)
    ///
    /// Example:
    /// [WorldService(typeof(MobaEntityManager))]
    /// public sealed class MobaEntityManager : IService
    /// {
    ///     public MobaEntityManager(IEventBus eventBus) { ... }
    /// }
    /// </summary>
    public sealed class MobaServicesAutoModule : IWorldModule
    {
        /// <summary>
        /// Namespace prefixes to scan for service classes.
        /// Only classes in these namespaces will be auto-registered.
        /// </summary>
        public static readonly string[] TargetNamespacePrefixes = new[]
        {
            "AbilityKit.Demo.Moba.Services",
            "AbilityKit.Demo.Moba.Services.Search",
            "AbilityKit.Demo.Moba.Services.Snapshot",
            "AbilityKit.Demo.Moba.Services.Area",
            "AbilityKit.Demo.Moba.Snapshot",
            "AbilityKit.Demo.Moba.Combat",
            "AbilityKit.Demo.Moba.Skill",
            "AbilityKit.Demo.Moba.Buff",
            "AbilityKit.Demo.Moba.Movement",
            "AbilityKit.Demo.Moba.Triggering",
            "AbilityKit.Demo.Moba.Effect",
            "AbilityKit.Demo.Moba.Summon",
            "AbilityKit.Demo.Moba.Projectile",
            "AbilityKit.Demo.Moba.Config",
            "AbilityKit.Demo.Moba.Actor",
            "AbilityKit.Demo.Moba.Core",
            "AbilityKit.Demo.Moba.FrameSync",
            "AbilityKit.Demo.Moba.Rollback",
            "AbilityKit.Demo.Moba.Systems",
            "AbilityKit.Demo.Moba.Trace",
        };

        /// <summary>
        /// Creates a new MobaServicesAutoModule that scans all loaded assemblies.
        /// </summary>
        public MobaServicesAutoModule()
        {
        }

        public void Configure(WorldContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            // Delegate to the framework's AttributeWorldServicesModule
            // This handles all the complexity of scanning and registration
            builder.AddModule(new AttributeWorldServicesModule(
                WorldServiceProfile.All,
                scanAllLoadedAssemblies: true,
                namespacePrefixes: TargetNamespacePrefixes
            ));
        }
    }
}
