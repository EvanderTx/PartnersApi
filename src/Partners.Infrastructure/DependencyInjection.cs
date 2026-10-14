using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Partners.Application.Partners;
using Partners.Infrastructure.Persistence;
using Partners.Infrastructure.Persistence.Repositories;

namespace Partners.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IPartnerRepository, PartnerRepository>();
        return services;
    }
}
