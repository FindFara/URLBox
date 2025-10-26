using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using URLBox.Domain.Authorization;
using URLBox.Domain.Entities;
using URLBox.Infrastructure.Persistance;

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

        private const string DefaultTestRole = "TestProjectRole";
        private const string DefaultTestProject = "Test Project";

        public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
        {
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;

            var roleManager = provider.GetRequiredService<RoleManager<ApplicationRole>>();
            var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
            var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");
            var dbContext = provider.GetRequiredService<ApplicationDbContext>();

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

            var configuredTestRoleName = configuration.GetSection("SeedData:TestRole").Value ?? DefaultTestRole;
            var configuredTestProjectName = configuration.GetSection("SeedData:TestProject").Value ?? DefaultTestProject;

            ApplicationRole? testRole = await roleManager.FindByNameAsync(configuredTestRoleName);
            if (testRole is null)
            {
                var createTestRoleResult = await roleManager.CreateAsync(new ApplicationRole { Name = configuredTestRoleName });
                if (!createTestRoleResult.Succeeded)
                {
                    var errors = string.Join(" ", createTestRoleResult.Errors.Select(e => e.Description));
                    logger.LogError("Failed to create test role {Role}: {Errors}", configuredTestRoleName, errors);
                }
                else
                {
                    testRole = await roleManager.FindByNameAsync(configuredTestRoleName);
                }
            }

            if (testRole is not null)
            {
                var project = await dbContext.Projects
                    .Include(p => p.Roles)
                    .FirstOrDefaultAsync(p => p.Name == configuredTestProjectName);

                if (project is null)
                {
                    project = new Project { Name = configuredTestProjectName };
                    dbContext.Projects.Add(project);
                }

                if (dbContext.Entry(testRole).State == EntityState.Detached)
                {
                    dbContext.Attach(testRole);
                }

                if (!project.Roles.Any(role => role.Id == testRole.Id))
                {
                    project.Roles.Add(testRole);
                }

                await dbContext.SaveChangesAsync();
                logger.LogInformation("Ensured test project {Project} is linked to role {Role}", configuredTestProjectName, configuredTestRoleName);
            }
        }
    }
}
