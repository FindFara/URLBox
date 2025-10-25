using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using URLBox.Application;
using URLBox.Domain.Entities;
using URLBox.Infrastructure;
using URLBox.Infrastructure.Persistance;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.RegisterApplicationServices(builder.Configuration);
builder.Services.RegisterInfrastructureServices(builder.Configuration);

var app = builder.Build();

await SeedIdentityAsync(app.Services, builder.Configuration);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static async Task SeedIdentityAsync(IServiceProvider services, IConfiguration configuration)
{
    using var scope = services.CreateScope();
    var scopedProvider = scope.ServiceProvider;

    var context = scopedProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();

    var roleManager = scopedProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<ApplicationRole>>();
    var userManager = scopedProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();

    var defaultRoles = new[] { "Admin", "DirectDebit", "AsaPay" };
    foreach (var roleName in defaultRoles)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
        }
    }

    var defaultProjects = new[] { "DirectDebit", "AsaPay" };
    foreach (var projectName in defaultProjects)
    {
        if (!await context.Projects.AnyAsync(p => p.Name == projectName))
        {
            context.Projects.Add(new Project { Name = projectName });
        }
    }

    await context.SaveChangesAsync();

    var adminSection = configuration.GetSection("AdminUser");
    var adminUserName = adminSection["UserName"];
    var adminEmail = adminSection["Email"];
    var adminPassword = adminSection["Password"];

    if (string.IsNullOrWhiteSpace(adminUserName) || string.IsNullOrWhiteSpace(adminPassword))
    {
        return;
    }

    var adminUser = await userManager.FindByNameAsync(adminUserName);
    if (adminUser is null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminUserName,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create admin user: {string.Join(";", result.Errors.Select(e => e.Description))}");
        }
    }

    if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}
