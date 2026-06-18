namespace AbilityKit.Orleans.Contracts.Rooms;

/// <summary>
/// Shared gameplay room type identifiers used by gateway, room grains and battle runtime selection.
/// </summary>
public static class GameplayRoomTypes
{
    public const string Moba = "battle";

    public const string Default = Moba;
}
