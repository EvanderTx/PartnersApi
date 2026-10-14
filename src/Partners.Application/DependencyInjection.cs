using Microsoft.Extensions.DependencyInjection;
using Partners.Application.Partners;

namespace Partners.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<PartnerService>();
        return services;
    }
}
