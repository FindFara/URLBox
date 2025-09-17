using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using URLBox.Domain.Interfaces;
using URLBox.Infrastructure.Persistance;
using URLBox.Infrastructure.Repositories;

namespace URLBox.Infrastructure;
public static class ConfigureServices
{
    public static IServiceCollection RegisterInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IUrlRepository, UrlRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ITeamRepository, TeamRepository>();


        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("URLBoxConnection")));

        return services;

    }
}
