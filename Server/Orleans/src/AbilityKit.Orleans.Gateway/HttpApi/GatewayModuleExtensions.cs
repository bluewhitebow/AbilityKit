namespace AbilityKit.Orleans.Gateway.HttpApi;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AbilityKit.Orleans.Hosting;

public static class GatewayModuleExtensions
{
    public static IServiceCollection AddAbilityKitGatewayModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AbilityKitGatewayOptions>()
            .Bind(configuration.GetSection(AbilityKitServerConfigurationSections.Gateway));

        services.AddGatewayHttpApi();
        return services;
    }

    public static WebApplication MapAbilityKitGatewayPipeline(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<AbilityKitGatewayOptions>>().Value;
        app.MapAbilityKitGatewayHealthEndpoints(options);
        app.MapGatewayHttpApi();
        return app;
    }
}
