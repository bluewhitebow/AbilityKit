using AbilityKit.Network.Runtime.Sync;
using Xunit;

namespace AbilityKit.Network.Runtime.Tests;

public sealed class NetworkSyncProfileControllerRegistryTests
{
    [Fact]
    public void CreateUsesDefaultProfileBuilderAndPassesTypedContext()
    {
        var registry = new NetworkSyncProfileControllerRegistry<string, TestContext>(
            new Dictionary<NetworkSyncProfile, NetworkSyncProfileControllerBuilder<string, TestContext>>
            {
                [NetworkSyncProfiles.PredictRollback] = BuildPredictRollback
            });
        var context = new TestContext("shooter", 60);

        var controller = registry.Create(NetworkSyncProfiles.PredictRollback, in context, "test controller");

        Assert.Equal("shooter:60:PredictRollback", controller);
        Assert.True(registry.Supports(NetworkSyncModel.PredictRollback));
        Assert.True(registry.Supports(NetworkSyncProfiles.PredictRollback));
        Assert.Equal(1, registry.Count);
    }

    [Fact]
    public void RegisterCanOverrideLegacyModelAndResetToDefaults()
    {
        var registry = new NetworkSyncProfileControllerRegistry<string, TestContext>(
            new Dictionary<NetworkSyncProfile, NetworkSyncProfileControllerBuilder<string, TestContext>>
            {
                [NetworkSyncProfiles.PredictRollback] = BuildPredictRollback
            });
        var context = new TestContext("moba", 30);

        registry.Register(NetworkSyncModel.AuthoritativeInterpolation, BuildAuthoritativeInterpolation);
        registry.Register(NetworkSyncProfiles.PredictRollback, BuildOverride);

        Assert.Equal("override:moba:30", registry.Create(NetworkSyncModel.PredictRollback, in context));
        Assert.Equal("moba:30:AuthoritativeInterpolation", registry.Create(NetworkSyncModel.AuthoritativeInterpolation, in context));
        Assert.Equal(2, registry.Count);

        registry.ResetToDefaults();

        Assert.Equal("moba:30:PredictRollback", registry.Create(NetworkSyncModel.PredictRollback, in context));
        Assert.False(registry.Supports(NetworkSyncModel.AuthoritativeInterpolation));
        Assert.Throws<NotSupportedException>(() => registry.Create(NetworkSyncModel.AuthoritativeInterpolation, in context, "sample controller"));
        Assert.Equal(1, registry.Count);
    }

    [Fact]
    public void ConstructorAndRegisterRejectNullBuilders()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new NetworkSyncProfileControllerRegistry<string, TestContext>(null!));

        Assert.Throws<ArgumentException>(() =>
            new NetworkSyncProfileControllerRegistry<string, TestContext>(
                new Dictionary<NetworkSyncProfile, NetworkSyncProfileControllerBuilder<string, TestContext>?>
                {
                    [NetworkSyncProfiles.PredictRollback] = null
                }!));

        var registry = new NetworkSyncProfileControllerRegistry<string, TestContext>(
            new Dictionary<NetworkSyncProfile, NetworkSyncProfileControllerBuilder<string, TestContext>>
            {
                [NetworkSyncProfiles.PredictRollback] = BuildPredictRollback
            });

        Assert.Throws<ArgumentNullException>(() => registry.Register(NetworkSyncProfiles.PredictRollback, null!));
    }

    private static string BuildPredictRollback(in TestContext context)
    {
        return $"{context.Name}:{context.TickRate}:PredictRollback";
    }

    private static string BuildAuthoritativeInterpolation(in TestContext context)
    {
        return $"{context.Name}:{context.TickRate}:AuthoritativeInterpolation";
    }

    private static string BuildOverride(in TestContext context)
    {
        return $"override:{context.Name}:{context.TickRate}";
    }

    private readonly struct TestContext
    {
        public TestContext(string name, int tickRate)
        {
            Name = name;
            TickRate = tickRate;
        }

        public string Name { get; }

        public int TickRate { get; }
    }
}
