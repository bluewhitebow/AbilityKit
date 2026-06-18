namespace AbilityKit.Orleans.Gateway.Handlers;

internal static class RoomGatewayErrorMapper
{
    public static GatewayResponse ToResponse(uint seq, Exception exception)
    {
        return GatewayResponse.Error(seq, RoomOperationErrorClassifier.ToError(exception).GatewayStatusCode);
    }

}
