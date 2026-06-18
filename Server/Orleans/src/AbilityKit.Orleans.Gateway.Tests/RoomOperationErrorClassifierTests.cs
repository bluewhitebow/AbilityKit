using AbilityKit.Orleans.Gateway;
using Xunit;

namespace AbilityKit.Orleans.Gateway.Tests;

public sealed class RoomOperationErrorClassifierTests
{
    [Theory]
    [InlineData("Room is full", RoomGatewayErrorCodes.RoomFull, 409, GatewayStatusCode.Conflict)]
    [InlineData("Room is closed", RoomGatewayErrorCodes.RoomClosed, 409, GatewayStatusCode.Conflict)]
    [InlineData("Account is not in room", RoomGatewayErrorCodes.AccountNotInRoom, 403, GatewayStatusCode.Forbidden)]
    [InlineData("Only owner can start battle", RoomGatewayErrorCodes.OwnerRequired, 403, GatewayStatusCode.Forbidden)]
    [InlineData("Unsupported MOBA room gameplay command", RoomGatewayErrorCodes.InvalidGameplayCommand, 400, GatewayStatusCode.BadRequest)]
    public void ToError_WhenInvalidOperationMatchesKnownRoomFailure_MapsHttpAndGatewayStatus(
        string message,
        string expectedCode,
        int expectedHttpStatusCode,
        int expectedGatewayStatusCode)
    {
        var error = RoomOperationErrorClassifier.ToError(new InvalidOperationException(message));

        Assert.Equal(expectedCode, error.Code);
        Assert.Equal(message, error.Message);
        Assert.Equal(expectedHttpStatusCode, error.HttpStatusCode);
        Assert.Equal(expectedGatewayStatusCode, error.GatewayStatusCode);
    }

    [Fact]
    public void ToError_WhenArgumentException_ReturnsBadRequestForBothTransports()
    {
        var error = RoomOperationErrorClassifier.ToError(new ArgumentException("payload is invalid"));

        Assert.Equal(RoomGatewayErrorCodes.BadRequest, error.Code);
        Assert.Equal(400, error.HttpStatusCode);
        Assert.Equal(GatewayStatusCode.BadRequest, error.GatewayStatusCode);
    }

    [Fact]
    public void ToError_WhenUnknownException_ReturnsInternalErrorForBothTransports()
    {
        var error = RoomOperationErrorClassifier.ToError(new Exception("boom"));

        Assert.Equal(RoomGatewayErrorCodes.InternalError, error.Code);
        Assert.Equal("Room operation failed.", error.Message);
        Assert.Equal(500, error.HttpStatusCode);
        Assert.Equal(GatewayStatusCode.InternalError, error.GatewayStatusCode);
    }
}
