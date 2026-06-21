using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AbilityKit.Orleans.Gateway.HttpApi;

internal static class GatewayEndpointHelpers
{
    public static async Task<IResult> ExecuteRoomOperationAsync(Func<Task<IResult>> operation)
    {
        try
        {
            return await operation();
        }
        catch (Exception exception)
        {
            return ToRoomHttpError(exception);
        }
    }

    public static IResult ToRoomHttpError(Exception exception)
    {
        return RoomHttpErrorMapper.ToResult(exception);
    }

    public static IResult ToRoomHttpError(string code, string message, int httpStatusCode, int gatewayStatusCode)
    {
        return RoomHttpErrorMapper.ToResult(code, message, httpStatusCode, gatewayStatusCode);
    }
}
