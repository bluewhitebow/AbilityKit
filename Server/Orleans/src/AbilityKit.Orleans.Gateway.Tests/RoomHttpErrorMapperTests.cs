using System.Text.Json;
using AbilityKit.Orleans.Gateway.HttpApi;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AbilityKit.Orleans.Gateway.Tests;

public sealed class RoomHttpErrorMapperTests : GatewayTestBase
{
    [Fact]
    public async Task ToResult_WhenRoomIsFull_ReturnsConflictPayloadWithGatewayStatus()
    {
        var result = RoomHttpErrorMapper.ToResult(new InvalidOperationException("Room is full"));
        var (context, body) = CreateHttpContext();
        await using (body)
        {
            await result.ExecuteAsync(context);

            Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
            body.Position = 0;
            using var document = await JsonDocument.ParseAsync(body);
            AssertHttpErrorMapping(
                document.RootElement,
                RoomGatewayErrorCodes.RoomFull,
                "Room is full",
                GatewayStatusCode.Conflict);
        }
    }

    [Fact]
    public async Task ToResult_WhenUnknownException_ReturnsInternalErrorPayloadWithGatewayStatus()
    {
        var result = RoomHttpErrorMapper.ToResult(new Exception("boom"));
        var (context, body) = CreateHttpContext();
        await using (body)
        {
            await result.ExecuteAsync(context);

            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            body.Position = 0;
            using var document = await JsonDocument.ParseAsync(body);
            AssertHttpErrorMapping(
                document.RootElement,
                RoomGatewayErrorCodes.InternalError,
                "Room operation failed.",
                GatewayStatusCode.InternalError);
        }
    }

    [Fact]
    public async Task ToResult_WhenExplicitRestoreRoomError_ReturnsNotFoundPayloadWithGatewayStatus()
    {
        var result = RoomHttpErrorMapper.ToResult(
            RoomGatewayErrorCodes.AccountNotInRoom,
            "No current room for account.",
            StatusCodes.Status404NotFound,
            GatewayStatusCode.NotFound);
        var (context, body) = CreateHttpContext();
        await using (body)
        {
            await result.ExecuteAsync(context);

            Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
            body.Position = 0;
            using var document = await JsonDocument.ParseAsync(body);
            AssertHttpErrorMapping(
                document.RootElement,
                RoomGatewayErrorCodes.AccountNotInRoom,
                "No current room for account.",
                GatewayStatusCode.NotFound);
        }
    }

    private static (DefaultHttpContext Context, MemoryStream Body) CreateHttpContext()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();
        var body = new MemoryStream();
        var context = new DefaultHttpContext
        {
            RequestServices = services
        };
        context.Response.Body = body;
        return (context, body);
    }
}
