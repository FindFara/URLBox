using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using URLBox.Domain.Authorization;
using URLBox.Domain.Entities;

namespace URLBox.Infrastructure.Seed
{
    public static class IdentitySeeder
    {
        private static readonly string[] DefaultRoles = new[]
        {
            AppRoles.Administrator,
            AppRoles.Manager,
            AppRoles.Viewer,
        };

        public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
        {
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;

            var roleManager = provider.GetRequiredService<RoleManager<ApplicationRole>>();
            var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
            var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");

            foreach (var role in DefaultRoles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var result = await roleManager.CreateAsync(new ApplicationRole { Name = role });
                    if (!result.Succeeded)
                    {
                        var errors = string.Join(" ", result.Errors.Select(e => e.Description));
                        logger.LogError("Failed to create role {Role}: {Errors}", role, errors);
                    }
                }
            }

            var adminUserName = configuration.GetSection("AdminUser:UserName").Value ?? "admin";
            var adminEmail = configuration.GetSection("AdminUser:Email").Value ?? "admin@example.com";
            var adminPassword = configuration.GetSection("AdminUser:Password").Value ?? "Admin123!";

            var adminUser = await userManager.FindByNameAsync(adminUserName);
            if (adminUser is null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminUserName,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(adminUser, adminPassword);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(" ", createResult.Errors.Select(e => e.Description));
                    logger.LogError("Failed to create admin user {UserName}: {Errors}", adminUserName, errors);
                    return;
                }

                logger.LogInformation("Created admin user {UserName}", adminUserName);
            }

            if (!await userManager.IsInRoleAsync(adminUser, AppRoles.Administrator))
            {
                var addRoleResult = await userManager.AddToRoleAsync(adminUser, AppRoles.Administrator);
                if (!addRoleResult.Succeeded)
                {
                    var errors = string.Join(" ", addRoleResult.Errors.Select(e => e.Description));
                    logger.LogError("Failed to assign Administrator role to {UserName}: {Errors}", adminUserName, errors);
                }
                else
                {
                    logger.LogInformation("Administrator role assigned to {UserName}", adminUserName);
                }
            }
        }
    }
}
