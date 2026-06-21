namespace AbilityKit.Orleans.Gateway.HttpApi;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Orleans;
using AbilityKit.Orleans.Contracts.Accounts;
using AbilityKit.Orleans.Contracts.Automation;
using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Contracts.Rooms;
using AbilityKit.Orleans.Contracts.FrameSync;

internal static class GatewayHttpApi
{
    private static readonly IReadOnlyList<string> Gameplays = new[]
    {
        "moba",
        "shooter"
    };

    public static void MapGatewayHttpApi(this WebApplication app)
    {
        app.MapGatewaySessionEndpoints();
        app.MapGatewaySandboxEndpoints();
        app.MapGatewayRoomEndpoints();
    }

    private static void MapGatewaySessionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/session")
            .WithTags("Session");

        group.MapPost("/guest/login", async (IClusterClient client) =>
        {
            var session = client.GetGrain<ISessionGrain>("global");
            var resp = await session.CreateGuestAsync();
            return Results.Ok(resp);
        })
        .WithName("Gateway.CreateGuestSession")
        .Produces(StatusCodes.Status200OK);

        group.MapPost("/accounts/login", async (AccountLoginRequest wire, IClusterClient client) =>
        {
            if (string.IsNullOrWhiteSpace(wire.AccountId))
            {
                return Results.BadRequest("AccountId is required");
            }

            var session = client.GetGrain<ISessionGrain>("global");
            try
            {
                var resp = await session.CreateSessionForAccountAsync(new CreateSessionForAccountRequest(
                    wire.AccountId,
                    wire.ExpireSeconds,
                    wire.KickExisting));
                return Results.Ok(resp);
            }
            catch (Exception exception)
            {
                return RoomHttpErrorMapper.ToResult(exception);
            }
        })
        .WithName("Gateway.LoginAccount")
        .Accepts<AccountLoginRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/validate", async (SessionTokenRequest wire, IClusterClient client) =>
        {
            if (string.IsNullOrWhiteSpace(wire.SessionToken))
            {
                return Results.BadRequest("SessionToken is required");
            }

            var session = client.GetGrain<ISessionGrain>("global");
            var resp = await session.ValidateAsync(new ValidateSessionRequest(wire.SessionToken));
            return Results.Ok(resp);
        })
        .WithName("Gateway.ValidateSession")
        .Accepts<SessionTokenRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/renew", async (RenewSessionWireRequest wire, IClusterClient client) =>
        {
            if (string.IsNullOrWhiteSpace(wire.SessionToken))
            {
                return Results.BadRequest("SessionToken is required");
            }

            var session = client.GetGrain<ISessionGrain>("global");
            var resp = await session.RenewAsync(new RenewSessionRequest(
                wire.SessionToken,
                wire.ExtendSeconds,
                wire.RotateToken));
            return Results.Ok(resp);
        })
        .WithName("Gateway.RenewSession")
        .Accepts<RenewSessionWireRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/logout", async (SessionTokenRequest wire, IClusterClient client) =>
        {
            if (string.IsNullOrWhiteSpace(wire.SessionToken))
            {
                return Results.BadRequest("SessionToken is required");
            }

            var accountId = await ValidateAccountAsync(client, wire.SessionToken);
            if (!string.IsNullOrWhiteSpace(accountId))
            {
                await MarkCurrentRoomOfflineAsync(client, accountId);
            }

            var session = client.GetGrain<ISessionGrain>("global");
            var resp = await session.LogoutAsync(new LogoutRequest(wire.SessionToken));
            return Results.Ok(resp);
        })
        .WithName("Gateway.LogoutSession")
        .Accepts<SessionTokenRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);
    }

    private static void MapGatewaySandboxEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api")
            .WithTags("Gameplay", "Sandbox");

        group.MapGet("/gameplays", () => Results.Ok(Gameplays))
            .WithName("Gateway.ListGameplays")
            .Produces(StatusCodes.Status200OK);

        group.MapPost("/shooter-sandbox/start", async (StartShooterSandboxRequest wire, IClusterClient client) =>
        {
            var sandbox = client.GetGrain<IShooterSandboxGrain>(string.IsNullOrWhiteSpace(wire.ServerId) ? "default" : wire.ServerId);
            var resp = await sandbox.StartAsync(wire);
            return Results.Ok(resp);
        })
        .WithName("Gateway.StartShooterSandbox")
        .Accepts<StartShooterSandboxRequest>("application/json")
        .Produces(StatusCodes.Status200OK);

        group.MapGet("/shooter-sandbox/{serverId?}", async (string? serverId, IClusterClient client) =>
        {
            var sandbox = client.GetGrain<IShooterSandboxGrain>(string.IsNullOrWhiteSpace(serverId) ? "default" : serverId);
            var resp = await sandbox.GetStateAsync();
            return Results.Ok(resp);
        })
        .WithName("Gateway.GetShooterSandboxState")
        .Produces(StatusCodes.Status200OK);

        group.MapPost("/shooter-sandbox/stop", async (string? serverId, IClusterClient client) =>
        {
            var sandbox = client.GetGrain<IShooterSandboxGrain>(string.IsNullOrWhiteSpace(serverId) ? "default" : serverId);
            await sandbox.StopAsync();
            return Results.Ok(new { Success = true });
        })
        .WithName("Gateway.StopShooterSandbox")
        .Produces(StatusCodes.Status200OK);
    }

    private static async Task<string?> ValidateAccountAsync(IClusterClient client, string sessionToken)
    {
        var session = client.GetGrain<ISessionGrain>("global");
        var result = await session.ValidateAsync(new ValidateSessionRequest(sessionToken));
        return result.IsValid ? result.AccountId : null;
    }

    private static async Task MarkCurrentRoomOfflineAsync(IClusterClient client, string accountId)
    {
        var mapping = client.GetGrain<IRoomIdMappingGrain>("global");
        var roomId = await mapping.TryGetAccountRoomAsync(accountId);
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return;
        }

        var room = client.GetGrain<IRoomGrain>(roomId);
        await room.MarkOfflineAsync(accountId);
    }
}
