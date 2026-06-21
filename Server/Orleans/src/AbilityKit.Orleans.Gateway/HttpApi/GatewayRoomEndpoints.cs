namespace AbilityKit.Orleans.Gateway.HttpApi;

using System.Collections.Generic;
using AbilityKit.Orleans.Contracts.Accounts;
using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Contracts.Rooms;
using Orleans;

public static class GatewayRoomEndpoints
{
    public static RouteGroupBuilder MapGatewayRoomEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/rooms")
            .WithTags("Rooms");

        group.MapPost("/create", (CreateRoomRequest request, IClusterClient client) =>
            GatewayEndpointHelpers.ExecuteRoomOperationAsync(async () =>
            {
                var directory = client.GetGrain<IRoomDirectoryGrain>($"{request.Region}:{request.ServerId}");
                var response = await directory.CreateRoomAsync(request);
                return Results.Ok(response);
            }))
        .WithName("Gateway.CreateRoom")
        .Accepts<CreateRoomRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/list", (ListRoomsRequest request, IClusterClient client) =>
            GatewayEndpointHelpers.ExecuteRoomOperationAsync(async () =>
            {
                var directory = client.GetGrain<IRoomDirectoryGrain>($"{request.Region}:{request.ServerId}");
                var response = await directory.ListRoomsAsync(request);
                return Results.Ok(response);
            }))
        .WithName("Gateway.ListRooms")
        .Accepts<ListRoomsRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/join", (JoinRoomRequest request, IClusterClient client) =>
            GatewayEndpointHelpers.ExecuteRoomOperationAsync(async () =>
            {
                var room = client.GetGrain<IRoomGrain>(request.RoomId);
                var response = await room.JoinAsync(request.AccountId);
                return Results.Ok(response);
            }))
        .WithName("Gateway.JoinRoom")
        .Accepts<JoinRoomRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/snapshot", (string roomId, IClusterClient client) =>
            GatewayEndpointHelpers.ExecuteRoomOperationAsync(async () =>
            {
                var room = client.GetGrain<IRoomGrain>(roomId);
                var snapshot = await room.GetSnapshotAsync();
                return Results.Ok(snapshot);
            }))
        .WithName("Gateway.GetRoomSnapshot")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/restore-current", (SessionTokenRequest request, IClusterClient client) =>
            GatewayEndpointHelpers.ExecuteRoomOperationAsync(async () =>
            {
                var session = client.GetGrain<ISessionGrain>("global");
                var validation = await session.ValidateAsync(new ValidateSessionRequest(request.SessionToken));
                if (!validation.IsValid || string.IsNullOrWhiteSpace(validation.AccountId))
                {
                    return GatewayEndpointHelpers.ToRoomHttpError(
                        RoomGatewayErrorCodes.BadRequest,
                        "Invalid session",
                        StatusCodes.Status400BadRequest,
                        GatewayStatusCode.BadRequest);
                }

                var mapping = client.GetGrain<IRoomIdMappingGrain>("global");
                var roomId = await mapping.TryGetAccountRoomAsync(validation.AccountId);
                if (string.IsNullOrWhiteSpace(roomId))
                {
                    return GatewayEndpointHelpers.ToRoomHttpError(
                        RoomGatewayErrorCodes.AccountNotInRoom,
                        "No current room for account.",
                        StatusCodes.Status404NotFound,
                        GatewayStatusCode.NotFound);
                }

                var room = client.GetGrain<IRoomGrain>(roomId);
                var snapshot = await room.GetSnapshotAsync();
                return Results.Ok(snapshot);
            }))
        .WithName("Gateway.RestoreCurrentRoom")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/leave", (LeaveRoomRequest request, IClusterClient client) =>
            GatewayEndpointHelpers.ExecuteRoomOperationAsync(async () =>
            {
                var room = client.GetGrain<IRoomGrain>(request.RoomId);
                await room.LeaveAsync(request.AccountId);
                return Results.Ok(new { Success = true });
            }))
        .WithName("Gateway.LeaveRoom")
        .Accepts<LeaveRoomRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapGet("/runtime-state/{roomId}", (string roomId, IClusterClient client) =>
            GatewayEndpointHelpers.ExecuteRoomOperationAsync(async () =>
            {
                var room = client.GetGrain<IRoomGrain>(roomId);
                var state = await room.GetRuntimeStateAsync();
                return Results.Ok(state);
            }))
        .WithName("Gateway.GetRoomRuntimeState")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/ready", (RoomReadyRequest request, IClusterClient client) =>
            GatewayEndpointHelpers.ExecuteRoomOperationAsync(async () =>
            {
                var room = client.GetGrain<IRoomGrain>(request.AccountId);
                await room.SetReadyAsync(request);
                var snapshot = await room.GetSnapshotAsync();
                return Results.Ok(snapshot);
            }))
        .WithName("Gateway.SetRoomReady")
        .Accepts<RoomReadyRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/pick-hero", (RoomPickHeroRequest request, IClusterClient client) =>
            GatewayEndpointHelpers.ExecuteRoomOperationAsync(async () =>
            {
                var room = client.GetGrain<IRoomGrain>(request.AccountId);
                await room.SetReadyAsync(new RoomReadyRequest(request.AccountId, true));
                var snapshot = await room.GetSnapshotAsync();
                return Results.Ok(snapshot);
            }))
        .WithName("Gateway.PickRoomHero")
        .Accepts<RoomPickHeroRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/start-battle", (StartRoomBattleRequest request, IClusterClient client) =>
            GatewayEndpointHelpers.ExecuteRoomOperationAsync(async () =>
            {
                var room = client.GetGrain<IRoomGrain>(request.AccountId);
                var response = await room.StartBattleAsync(request);
                return Results.Ok(response);
            }))
        .WithName("Gateway.StartRoomBattle")
        .Accepts<StartRoomBattleRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        return group;
    }
}
