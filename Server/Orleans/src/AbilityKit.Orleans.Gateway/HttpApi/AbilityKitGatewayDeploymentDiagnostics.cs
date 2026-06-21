using System.Collections.Generic;

namespace AbilityKit.Orleans.Gateway.HttpApi;

internal sealed record AbilityKitGatewayDeploymentDiagnostics(
    string Role,
    string Region,
    string ServerId,
    string? Cluster,
    string? NodeName,
    string? SiloId,
    AbilityKitGatewayRuntimeDiagnostics? Runtime,
    AbilityKitGatewayGameplayDiagnostics? Gameplay);

internal sealed record AbilityKitGatewayRuntimeDiagnostics(
    string RootPath,
    string EnvironmentName,
    bool IsDevelopment,
    string? ReleaseVersion);

internal sealed record AbilityKitGatewayGameplayDiagnostics(
    int ModuleCount,
    IReadOnlyList<string> RoomTypes,
    IReadOnlyList<string> BattleProfiles);
