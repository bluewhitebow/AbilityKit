using Orleans.Serialization;

namespace AbilityKit.Orleans.Contracts.Accounts;

[GenerateSerializer]
public sealed record AccountLoginRequest(
    [property: Id(0)] string AccountId,
    [property: Id(1)] int ExpireSeconds = 0,
    [property: Id(2)] bool KickExisting = false);

[GenerateSerializer]
public sealed record SessionTokenRequest(
    [property: Id(0)] string SessionToken);

[GenerateSerializer]
public sealed record RenewSessionWireRequest(
    [property: Id(0)] string SessionToken,
    [property: Id(1)] int ExtendSeconds = 0,
    [property: Id(2)] bool RotateToken = false);
