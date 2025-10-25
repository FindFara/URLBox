using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using URLBox.Application.Services;

namespace URLBox.Application
{
    public static class ConfigureServices
    {
        public static IServiceCollection RegisterApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<UrlService>();
            services.AddScoped<ProjectService>();
            services.AddScoped<TeamService>();

            return services;
        }
    }
}
