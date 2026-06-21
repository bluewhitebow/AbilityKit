using Orleans.Serialization;

namespace AbilityKit.Orleans.Gateway.HttpApi;

[GenerateSerializer]
internal sealed record AccountLoginHttpRequest(
    [property: Id(0)] string AccountId,
    [property: Id(1)] int ExpireSeconds = 0,
    [property: Id(2)] bool KickExisting = false);

[GenerateSerializer]
internal sealed record SessionTokenHttpRequest(
    [property: Id(0)] string SessionToken);

[GenerateSerializer]
internal sealed record RenewSessionHttpRequest(
    [property: Id(0)] string SessionToken,
    [property: Id(1)] int ExtendSeconds = 0,
    [property: Id(2)] bool RotateToken = false);

[GenerateSerializer]
internal sealed record MarkRoomMemberOfflineRequest(
    [property: Id(0)] string AccountId,
    [property: Id(1)] string Reason);
