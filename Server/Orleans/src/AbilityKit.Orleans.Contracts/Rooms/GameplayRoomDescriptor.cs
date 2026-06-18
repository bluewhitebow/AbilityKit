using Orleans.Serialization;

namespace AbilityKit.Orleans.Contracts.Rooms;

[GenerateSerializer]
public sealed record GameplayRoomDescriptor(
    [property: Id(0)] string RoomType,
    [property: Id(1)] string DisplayName,
    [property: Id(2)] int DefaultMaxPlayers,
    [property: Id(3)] bool RequiresPlayerLoadout,
    [property: Id(4)] string? DefaultWorldType,
    [property: Id(5)] int DefaultTickRate,
    [property: Id(6)] string? DefaultSyncTemplateId);
