using System;
using System.Collections.Generic;
using System.Linq;
using AbilityKit.Orleans.Grains.Gameplay;

namespace AbilityKit.Orleans.Grains.Battle.Gameplay;

internal sealed class BattleRuntimeRegistry
{
    private readonly Dictionary<string, IBattleRuntimeAdapter> _adapters;
    private readonly IBattleRuntimeAdapter _defaultAdapter;

    public BattleRuntimeRegistry(IEnumerable<IBattleRuntimeAdapter> adapters, ServerGameplayCatalog catalog)
    {
        if (adapters is null)
        {
            throw new ArgumentNullException(nameof(adapters));
        }

        if (catalog is null)
        {
            throw new ArgumentNullException(nameof(catalog));
        }

        _adapters = adapters.ToDictionary(a => a.RoomType, StringComparer.OrdinalIgnoreCase);
        foreach (var roomType in _adapters.Keys)
        {
            catalog.EnsureRegistered(roomType);
        }

        var defaultRoomType = catalog.DefaultDescriptor.RoomType;
        if (!_adapters.TryGetValue(defaultRoomType, out _defaultAdapter!))
        {
            throw new InvalidOperationException($"Default battle runtime adapter is not registered. RoomType={defaultRoomType}");
        }
    }

    public IBattleRuntimeAdapter Resolve(string? roomType)
    {
        if (!string.IsNullOrWhiteSpace(roomType) && _adapters.TryGetValue(roomType, out var adapter))
        {
            return adapter;
        }

        return _defaultAdapter;
    }
}
