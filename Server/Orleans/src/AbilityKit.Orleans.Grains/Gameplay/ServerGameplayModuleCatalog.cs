using System;
using System.Collections.Generic;
using AbilityKit.Orleans.Contracts.Rooms;
using AbilityKit.Orleans.Grains.Battle;
using AbilityKit.Orleans.Grains.Battle.Gameplay;
using AbilityKit.Orleans.Grains.Gameplays.Moba.Battle;
using AbilityKit.Orleans.Grains.Gameplays.Moba.Protocol;
using AbilityKit.Orleans.Grains.Gameplays.Moba.Rooms;
using AbilityKit.Orleans.Grains.Gameplays.Shooter.Battle;
using AbilityKit.Orleans.Grains.Gameplays.Shooter.Rooms;
using AbilityKit.Orleans.Grains.Rooms.Gameplay;

namespace AbilityKit.Orleans.Grains.Gameplay;

internal sealed class ServerGameplayModule
{
    private readonly Func<IRoomGameplayAdapter> _roomAdapterFactory;
    private readonly Func<ServerBattleWorldManager, IBattleRuntimeAdapter> _battleRuntimeAdapterFactory;

    public ServerGameplayModule(
        GameplayRoomDescriptor descriptor,
        Func<IRoomGameplayAdapter> roomAdapterFactory,
        Func<ServerBattleWorldManager, IBattleRuntimeAdapter> battleRuntimeAdapterFactory)
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        _roomAdapterFactory = roomAdapterFactory ?? throw new ArgumentNullException(nameof(roomAdapterFactory));
        _battleRuntimeAdapterFactory = battleRuntimeAdapterFactory ?? throw new ArgumentNullException(nameof(battleRuntimeAdapterFactory));
    }

    public GameplayRoomDescriptor Descriptor { get; }

    public string RoomType => Descriptor.RoomType;

    public IRoomGameplayAdapter CreateRoomAdapter()
    {
        var adapter = _roomAdapterFactory();
        if (!string.Equals(adapter.RoomType, RoomType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Room gameplay adapter type mismatch. Descriptor={RoomType}, Adapter={adapter.RoomType}");
        }

        return adapter;
    }

    public IBattleRuntimeAdapter CreateBattleRuntimeAdapter(ServerBattleWorldManager worldManager)
    {
        var adapter = _battleRuntimeAdapterFactory(worldManager ?? throw new ArgumentNullException(nameof(worldManager)));
        if (!string.Equals(adapter.RoomType, RoomType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Battle runtime adapter type mismatch. Descriptor={RoomType}, Adapter={adapter.RoomType}");
        }

        return adapter;
    }
}

internal sealed class ServerGameplayModuleCatalog
{
    private readonly IReadOnlyList<ServerGameplayModule> _modules;

    public static ServerGameplayModuleCatalog Default { get; } = new(new[]
    {
        new ServerGameplayModule(
            ServerGameplayDescriptors.Moba,
            static () => new MobaRoomGameplayAdapter(),
            static worldManager => new MobaBattleRuntimeAdapter(worldManager, DefaultOrleansBattleProtocolMapper.Instance)),
        new ServerGameplayModule(
            ServerGameplayDescriptors.Shooter,
            static () => new ShooterRoomGameplayAdapter(),
            static worldManager => new ShooterBattleRuntimeAdapter(worldManager))
    });

    public ServerGameplayModuleCatalog(IReadOnlyList<ServerGameplayModule> modules)
    {
        if (modules is null)
        {
            throw new ArgumentNullException(nameof(modules));
        }

        if (modules.Count == 0)
        {
            throw new ArgumentException("At least one server gameplay module must be registered.", nameof(modules));
        }

        _modules = modules;
        GameplayCatalog = new ServerGameplayCatalog(GetDescriptors(modules));
    }

    public ServerGameplayCatalog GameplayCatalog { get; }

    public IReadOnlyList<ServerGameplayModule> Modules => _modules;

    public IReadOnlyList<IRoomGameplayAdapter> CreateRoomAdapters()
    {
        var adapters = new IRoomGameplayAdapter[_modules.Count];
        for (var i = 0; i < _modules.Count; i++)
        {
            adapters[i] = _modules[i].CreateRoomAdapter();
        }

        return adapters;
    }

    public IReadOnlyList<IBattleRuntimeAdapter> CreateBattleRuntimeAdapters(ServerBattleWorldManager worldManager)
    {
        var adapters = new IBattleRuntimeAdapter[_modules.Count];
        for (var i = 0; i < _modules.Count; i++)
        {
            adapters[i] = _modules[i].CreateBattleRuntimeAdapter(worldManager);
        }

        return adapters;
    }

    private static IReadOnlyList<GameplayRoomDescriptor> GetDescriptors(IReadOnlyList<ServerGameplayModule> modules)
    {
        var descriptors = new GameplayRoomDescriptor[modules.Count];
        for (var i = 0; i < modules.Count; i++)
        {
            descriptors[i] = modules[i].Descriptor;
        }

        return descriptors;
    }
}
