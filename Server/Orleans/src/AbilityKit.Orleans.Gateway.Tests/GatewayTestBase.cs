using System.Text.Json;
using Xunit;

namespace AbilityKit.Orleans.Gateway.Tests;

public abstract class GatewayTestBase
{
    protected static void AssertErrorMapping(
        dynamic error,
        string expectedCode,
        string expectedMessage,
        int expectedHttpStatusCode,
        int expectedGatewayStatusCode)
    {
        Assert.Equal(expectedCode, (string)error.Code);
        Assert.Equal(expectedMessage, (string)error.Message);
        Assert.Equal(expectedHttpStatusCode, (int)error.HttpStatusCode);
        Assert.Equal(expectedGatewayStatusCode, (int)error.GatewayStatusCode);
    }

    protected static void AssertHttpErrorMapping(
        dynamic error,
        string expectedCode,
        string expectedMessage,
        int expectedGatewayStatusCode)
    {
        Assert.Equal(expectedCode, (string)error.Code);
        Assert.Equal(expectedMessage, (string)error.Message);
        Assert.Equal(expectedGatewayStatusCode, (int)error.GatewayStatusCode);
    }

    protected static void AssertHttpErrorMapping(
        JsonElement error,
        string expectedCode,
        string expectedMessage,
        int expectedGatewayStatusCode)
    {
        Assert.Equal(expectedCode, error.GetProperty("code").GetString());
        Assert.Equal(expectedMessage, error.GetProperty("message").GetString());
        Assert.Equal(expectedGatewayStatusCode, error.GetProperty("gatewayStatusCode").GetInt32());
    }
}
