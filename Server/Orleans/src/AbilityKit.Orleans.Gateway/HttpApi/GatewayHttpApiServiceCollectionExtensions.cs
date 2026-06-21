using Microsoft.Extensions.DependencyInjection;

namespace AbilityKit.Orleans.Gateway.HttpApi;

internal static class GatewayHttpApiServiceCollectionExtensions
{
    public static IServiceCollection AddGatewayHttpApi(this IServiceCollection services)
    {
        return services;
    }
}
