using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RoutingService.Application.Abstractions;
using RoutingService.Application.Options;
using RoutingService.Infrastructure.Health;
using RoutingService.Infrastructure.Routing;

namespace RoutingService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRoutingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OsrmOptions>(configuration.GetSection(OsrmOptions.SectionName));

        services.AddHttpClient(OsrmRouteProvider.HttpClientName, (sp, client) =>
        {
            var opt = sp.GetRequiredService<IOptions<OsrmOptions>>().Value;
            client.BaseAddress = new Uri(opt.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromMilliseconds(Math.Max(1, opt.TimeoutMs));
        });

        services.AddSingleton<IRouteProvider, OsrmRouteProvider>();
        services.AddSingleton<OsrmReadinessHealthCheck>();

        return services;
    }
}
