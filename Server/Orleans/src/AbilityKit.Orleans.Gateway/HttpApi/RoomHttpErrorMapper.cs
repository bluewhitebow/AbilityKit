using Microsoft.AspNetCore.Http;

namespace AbilityKit.Orleans.Gateway.HttpApi;

internal static class RoomHttpErrorMapper
{
    public static IResult ToResult(Exception exception)
    {
        var error = RoomOperationErrorClassifier.ToError(exception);
        return ToResult(error.Code, error.Message, error.HttpStatusCode, error.GatewayStatusCode);
    }

    public static IResult ToResult(string code, string message, int httpStatusCode, int gatewayStatusCode)
    {
        return Results.Json(
            new RoomHttpErrorResponse(code, message, gatewayStatusCode),
            statusCode: httpStatusCode);
    }

    private sealed record RoomHttpErrorResponse(string Code, string Message, int GatewayStatusCode);
}
