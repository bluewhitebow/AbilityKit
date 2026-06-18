using System.Linq;
using AbilityKit.Demo.Shooter;
using AbilityKit.Orleans.Contracts.Rooms;
using AbilityKit.Orleans.Grains.Battle;
using AbilityKit.Orleans.Grains.Gameplay;
using AbilityKit.Orleans.Grains.Gameplays.Moba.Battle;
using AbilityKit.Orleans.Grains.Gameplays.Moba.Rooms;
using AbilityKit.Orleans.Grains.Gameplays.Shooter.Battle;
using AbilityKit.Orleans.Grains.Gameplays.Shooter.Rooms;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AbilityKit.Orleans.Grains.Tests.Battle;

public sealed class ServerGameplayModuleCatalogTests
{
    [Fact]
    public void DefaultCatalog_WhenCreatingAdapters_RegistersRoomAndBattleModulesAsPairs()
    {
        var moduleCatalog = ServerGameplayModuleCatalog.Default;
        using var worldManager = new ServerBattleWorldManager(NullLogger.Instance);

        var descriptors = moduleCatalog.GameplayCatalog.Descriptors.ToDictionary(d => d.RoomType);
        var roomAdapters = moduleCatalog.CreateRoomAdapters().ToDictionary(a => a.RoomType);
        var battleAdapters = moduleCatalog.CreateBattleRuntimeAdapters(worldManager).ToDictionary(a => a.RoomType);

        Assert.Contains(GameplayRoomTypes.Moba, descriptors.Keys);
        Assert.Contains(ShooterGameplay.RoomType, descriptors.Keys);
        Assert.Equal(descriptors.Keys.OrderBy(k => k), roomAdapters.Keys.OrderBy(k => k));
        Assert.Equal(descriptors.Keys.OrderBy(k => k), battleAdapters.Keys.OrderBy(k => k));
        Assert.IsType<MobaRoomGameplayAdapter>(roomAdapters[GameplayRoomTypes.Moba]);
        Assert.IsType<ShooterRoomGameplayAdapter>(roomAdapters[ShooterGameplay.RoomType]);
        Assert.IsType<MobaBattleRuntimeAdapter>(battleAdapters[GameplayRoomTypes.Moba]);
        Assert.IsType<ShooterBattleRuntimeAdapter>(battleAdapters[ShooterGameplay.RoomType]);
        Assert.Equal(GameplayRoomTypes.Moba, moduleCatalog.GameplayCatalog.DefaultDescriptor.RoomType);
    }
}
