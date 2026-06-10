using System;
using System.Reflection;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.Triggering.Runtime.Builtins;

namespace AbilityKit.Ability.Triggering.Runtime
{
    public sealed class TriggeringWorldModule : IWorldModule
    {
        private readonly Assembly[] _assemblies;
        private readonly string[] _namespacePrefixes;
        private readonly bool _scanAllLoadedAssemblies;

        public TriggeringWorldModule()
            : this(null, null, true)
        {
        }

        public TriggeringWorldModule(Assembly[] assemblies, string[] namespacePrefixes)
            : this(assemblies, namespacePrefixes, false)
        {
        }

        private TriggeringWorldModule(Assembly[] assemblies, string[] namespacePrefixes, bool scanAllLoadedAssemblies)
        {
            _assemblies = assemblies;
            _namespacePrefixes = namespacePrefixes;
            _scanAllLoadedAssemblies = scanAllLoadedAssemblies;
        }

        public void Configure(WorldContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.TryRegister<ITriggerContextFactory>(WorldLifetime.Scoped, services =>
            {
                var sp = new WorldServiceProviderAdapter(services);
                return new WorldTriggerContextFactory(sp);
            });

            builder.TryRegister<TriggerRegistry>(WorldLifetime.Scoped, _ =>
            {
                var registry = new TriggerRegistry();
                var assemblies = _assemblies;
                if ((assemblies == null || assemblies.Length == 0) && _scanAllLoadedAssemblies)
                {
                    assemblies = AppDomain.CurrentDomain.GetAssemblies();
                }

                if (assemblies != null && assemblies.Length > 0)
                {
                    registry.AutoRegisterFromAssemblies(assemblies, _namespacePrefixes);
                }

                return registry;
            });

            builder.TryRegister<TriggerRunner>(WorldLifetime.Scoped, services =>
            {
                var bus = services.Resolve<IEventBus>();
                var registry = services.Resolve<TriggerRegistry>();
                var ctxFactory = services.Resolve<ITriggerContextFactory>();
                return new TriggerRunner(bus, registry, ctxFactory);
            });
        }

    }
}
