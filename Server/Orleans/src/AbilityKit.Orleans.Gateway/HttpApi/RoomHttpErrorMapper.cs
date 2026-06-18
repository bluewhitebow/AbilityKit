using Microsoft.AspNetCore.Http;

namespace AbilityKit.Orleans.Gateway.HttpApi;

internal static class RoomHttpErrorMapper
{
    public static IResult ToResult(Exception exception)
    {
        var error = ToError(exception);
        return Results.Json(
            new RoomHttpErrorResponse(error.Code, error.Message),
            statusCode: error.HttpStatusCode);
    }

    private static RoomOperationError ToError(Exception exception)
    {
        return RoomOperationErrorClassifier.ToError(exception);
    }

    private sealed record RoomHttpErrorResponse(string Code, string Message);
}
