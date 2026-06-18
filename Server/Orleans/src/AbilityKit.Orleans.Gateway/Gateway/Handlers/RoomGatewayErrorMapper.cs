namespace AbilityKit.Orleans.Gateway.Handlers;

internal static class RoomGatewayErrorMapper
{
    public static GatewayResponse ToResponse(uint seq, Exception exception)
    {
        return GatewayResponse.Error(seq, ToStatusCode(exception));
    }

    private static int ToStatusCode(Exception exception)
    {
        return exception switch
        {
            ArgumentException => GatewayStatusCode.BadRequest,
            InvalidOperationException invalidOperationException => MapInvalidOperation(invalidOperationException),
            _ => GatewayStatusCode.InternalError
        };
    }

    private static int MapInvalidOperation(InvalidOperationException exception)
    {
        var message = exception.Message;
        if (message.Contains("Account is not in room", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Only owner", StringComparison.OrdinalIgnoreCase))
        {
            return GatewayStatusCode.Forbidden;
        }

        if (message.Contains("Unsupported MOBA room gameplay command", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Invalid MOBA room loadout command", StringComparison.OrdinalIgnoreCase)
            || message.Contains("loadout command fields are required", StringComparison.OrdinalIgnoreCase))
        {
            return GatewayStatusCode.BadRequest;
        }

        if (message.Contains("Room is full", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Room is closed", StringComparison.OrdinalIgnoreCase))
        {
            return GatewayStatusCode.Conflict;
        }

        return GatewayStatusCode.Conflict;
    }
}
