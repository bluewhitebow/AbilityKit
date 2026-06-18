using Microsoft.AspNetCore.Http;

namespace AbilityKit.Orleans.Gateway.HttpApi;

internal static class RoomHttpErrorMapper
{
    public static IResult ToResult(Exception exception)
    {
        var error = ToError(exception);
        return Results.Json(
            new RoomHttpErrorResponse(error.Code, error.Message),
            statusCode: error.StatusCode);
    }

    private static RoomHttpError ToError(Exception exception)
    {
        return exception switch
        {
            ArgumentException argumentException => new RoomHttpError(
                RoomHttpErrorCodes.BadRequest,
                argumentException.Message,
                StatusCodes.Status400BadRequest),
            InvalidOperationException invalidOperationException => MapInvalidOperation(invalidOperationException),
            _ => new RoomHttpError(
                RoomHttpErrorCodes.InternalError,
                "Room operation failed.",
                StatusCodes.Status500InternalServerError)
        };
    }

    private static RoomHttpError MapInvalidOperation(InvalidOperationException exception)
    {
        var message = exception.Message;
        if (message.Contains("Room is full", StringComparison.OrdinalIgnoreCase))
        {
            return new RoomHttpError(RoomHttpErrorCodes.RoomFull, message, StatusCodes.Status409Conflict);
        }

        if (message.Contains("Room is closed", StringComparison.OrdinalIgnoreCase))
        {
            return new RoomHttpError(RoomHttpErrorCodes.RoomClosed, message, StatusCodes.Status409Conflict);
        }

        if (message.Contains("Account is not in room", StringComparison.OrdinalIgnoreCase))
        {
            return new RoomHttpError(RoomHttpErrorCodes.AccountNotInRoom, message, StatusCodes.Status403Forbidden);
        }

        if (message.Contains("Only owner", StringComparison.OrdinalIgnoreCase))
        {
            return new RoomHttpError(RoomHttpErrorCodes.OwnerRequired, message, StatusCodes.Status403Forbidden);
        }

        if (message.Contains("Unsupported MOBA room gameplay command", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Invalid MOBA room loadout command", StringComparison.OrdinalIgnoreCase)
            || message.Contains("loadout command fields are required", StringComparison.OrdinalIgnoreCase))
        {
            return new RoomHttpError(RoomHttpErrorCodes.InvalidGameplayCommand, message, StatusCodes.Status400BadRequest);
        }

        return new RoomHttpError(RoomHttpErrorCodes.RoomOperationFailed, message, StatusCodes.Status409Conflict);
    }

    private sealed record RoomHttpError(string Code, string Message, int StatusCode);

    private sealed record RoomHttpErrorResponse(string Code, string Message);
}

internal static class RoomHttpErrorCodes
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
