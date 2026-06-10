using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Demo.Shooter.Runtime;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests.Worlds
{
    public sealed class ShooterServicesAutoModuleScanDomainTests
    {
        [Fact]
        public void ConfigureRegistersServicesInsideShooterNamespaceOnly()
        {
            var container = new WorldContainerBuilder()
                .AddModule(new ShooterServicesAutoModule(typeof(ShooterScanDomainAcceptedService).Assembly))
                .Build();

            Assert.True(container.TryResolve<ShooterScanDomainAcceptedService>(out _));
            Assert.False(container.TryResolve<AbilityKit.Demo.Moba.Tests.ForeignWorldService>(out _));
        }
    }

    [WorldService(typeof(ShooterScanDomainAcceptedService), WorldLifetime.Singleton)]
    public sealed class ShooterScanDomainAcceptedService
    {
    }
}

namespace AbilityKit.Demo.Moba.Tests
{
    [WorldService(typeof(ForeignWorldService), WorldLifetime.Singleton)]
    public sealed class ForeignWorldService
    {
    }
}
