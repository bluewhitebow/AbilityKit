using AbilityKit.Orleans.Contracts.Rooms;
using Orleans;
using Orleans.Serialization;

namespace AbilityKit.Orleans.Contracts.Automation;

public interface IShooterSandboxGrain : IGrainWithStringKey
{
    Task<ShooterSandboxState> StartAsync(StartShooterSandboxRequest request);

    Task<ShooterSandboxState> GetStateAsync();

    Task StopAsync();
}

[GenerateSerializer]
public sealed record StartShooterSandboxRequest(
    [property: Id(0)] string Region,
    [property: Id(1)] string ServerId,
    [property: Id(2)] int BotCount,
    [property: Id(3)] int MaxPlayers,
    [property: Id(4)] int TickRate,
    [property: Id(5)] string? Title,
    [property: Id(6)] Dictionary<string, string>? Tags);

[GenerateSerializer]
public sealed record ShooterSandboxState(
    [property: Id(0)] bool Running,
    [property: Id(1)] string Region,
    [property: Id(2)] string ServerId,
    [property: Id(3)] string RoomId,
    [property: Id(4)] string BattleId,
    [property: Id(5)] ulong WorldId,
    [property: Id(6)] int BotCount,
    [property: Id(7)] int CurrentFrame,
    [property: Id(8)] long ServerNowTicks,
    [property: Id(9)] RoomSnapshot? Snapshot);
