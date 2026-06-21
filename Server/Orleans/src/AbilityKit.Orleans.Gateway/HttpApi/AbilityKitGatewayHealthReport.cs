using System;
using System.Collections.Generic;

namespace AbilityKit.Orleans.Gateway.HttpApi;

internal sealed record AbilityKitGatewayHealthReport(
    string Status,
    string Service,
    string? Message,
    AbilityKitGatewayDeploymentDiagnostics Deployment,
    IReadOnlyList<string> Diagnostics,
    IReadOnlyList<string> Warnings)
{
    public static AbilityKitGatewayHealthReport Ready(
        string service,
        AbilityKitGatewayDeploymentDiagnostics deployment,
        IReadOnlyList<string>? diagnostics = null,
        IReadOnlyList<string>? warnings = null,
        string? message = null)
    {
        return new AbilityKitGatewayHealthReport(
            "Ready",
            service,
            message,
            deployment,
            diagnostics ?? Array.Empty<string>(),
            warnings ?? Array.Empty<string>());
    }

    public static AbilityKitGatewayHealthReport Degraded(
        string service,
        AbilityKitGatewayDeploymentDiagnostics deployment,
        IReadOnlyList<string>? diagnostics = null,
        IReadOnlyList<string>? warnings = null,
        string? message = null)
    {
        return new AbilityKitGatewayHealthReport(
            "Degraded",
            service,
            message,
            deployment,
            diagnostics ?? Array.Empty<string>(),
            warnings ?? Array.Empty<string>());
    }

    public static AbilityKitGatewayHealthReport Unhealthy(
        string service,
        AbilityKitGatewayDeploymentDiagnostics deployment,
        IReadOnlyList<string>? diagnostics = null,
        IReadOnlyList<string>? warnings = null,
        string? message = null)
    {
        return new AbilityKitGatewayHealthReport(
            "Unhealthy",
            service,
            message,
            deployment,
            diagnostics ?? Array.Empty<string>(),
            warnings ?? Array.Empty<string>());
    }
}
