using Microsoft.AspNetCore.Http;

namespace AbilityKit.Orleans.Gateway;

internal static class RoomOperationErrorClassifier
{
    public static RoomOperationError ToError(Exception exception)
    {
        return exception switch
        {
            ArgumentException argumentException => new RoomOperationError(
                RoomGatewayErrorCodes.BadRequest,
                argumentException.Message,
                StatusCodes.Status400BadRequest,
                GatewayStatusCode.BadRequest),
            InvalidOperationException invalidOperationException => MapInvalidOperation(invalidOperationException),
            _ => new RoomOperationError(
                RoomGatewayErrorCodes.InternalError,
                "Room operation failed.",
                StatusCodes.Status500InternalServerError,
                GatewayStatusCode.InternalError)
        };
    }

    private static RoomOperationError MapInvalidOperation(InvalidOperationException exception)
    {
        var message = exception.Message;
        if (message.Contains("Room is full", StringComparison.OrdinalIgnoreCase))
        {
            return new RoomOperationError(RoomGatewayErrorCodes.RoomFull, message, StatusCodes.Status409Conflict, GatewayStatusCode.Conflict);
        }

        if (message.Contains("Room is closed", StringComparison.OrdinalIgnoreCase))
        {
            return new RoomOperationError(RoomGatewayErrorCodes.RoomClosed, message, StatusCodes.Status409Conflict, GatewayStatusCode.Conflict);
        }

        if (message.Contains("Account is not in room", StringComparison.OrdinalIgnoreCase))
        {
            return new RoomOperationError(RoomGatewayErrorCodes.AccountNotInRoom, message, StatusCodes.Status403Forbidden, GatewayStatusCode.Forbidden);
        }

        if (message.Contains("Only owner", StringComparison.OrdinalIgnoreCase))
        {
            return new RoomOperationError(RoomGatewayErrorCodes.OwnerRequired, message, StatusCodes.Status403Forbidden, GatewayStatusCode.Forbidden);
        }

        if (message.Contains("Unsupported MOBA room gameplay command", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Invalid MOBA room loadout command", StringComparison.OrdinalIgnoreCase)
            || message.Contains("loadout command fields are required", StringComparison.OrdinalIgnoreCase))
        {
            return new RoomOperationError(RoomGatewayErrorCodes.InvalidGameplayCommand, message, StatusCodes.Status400BadRequest, GatewayStatusCode.BadRequest);
        }

        return new RoomOperationError(RoomGatewayErrorCodes.RoomOperationFailed, message, StatusCodes.Status409Conflict, GatewayStatusCode.Conflict);
    }
}

internal sealed record RoomOperationError(string Code, string Message, int HttpStatusCode, int GatewayStatusCode);

internal static class RoomGatewayErrorCodes
{
    public const string BadRequest = "bad_request";
    public const string RoomFull = "room_full";
    public const string RoomClosed = "room_closed";
    public const string AccountNotInRoom = "account_not_in_room";
    public const string OwnerRequired = "room_owner_required";
    public const string InvalidGameplayCommand = "invalid_room_gameplay_command";
    public const string RoomOperationFailed = "room_operation_failed";
    public const string InternalError = "internal_error";
}
