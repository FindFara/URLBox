using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using System.Threading.RateLimiting;
using URLBox.Domain.Interfaces;
using URLBox.Infrastructure.Storage;
using URLBox.Presentation.Authentication;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();

var directory = Path.GetFullPath(builder.Configuration["Storage:Directory"] ?? "App_Data/teams", builder.Environment.ContentRootPath);
var webRoot = Path.GetFullPath(builder.Environment.WebRootPath ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot"));
if (directory.Equals(webRoot, StringComparison.OrdinalIgnoreCase)
    || directory.StartsWith(webRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Team files must be stored outside wwwroot.");
Directory.CreateDirectory(directory);
builder.Services.AddSingleton<ITeamStore>(new YamlTeamStore(directory));
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(directory, ".keys")))
    .SetApplicationName("URLBox");
builder.Services.AddScoped<TeamCookieEvents>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "URLBox.YamlTeamAuth";
    options.Cookie.HttpOnly = true;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Home/Index";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
    options.EventsType = typeof(TeamCookieEvents);
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("team-login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "local",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

var app = builder.Build();
await app.Services.GetRequiredService<ITeamStore>().EnsureAdminAsync(
    builder.Configuration["AdminTeam:Name"] ?? "admin",
    builder.Configuration["AdminTeam:Password"] ?? throw new InvalidOperationException("Configure AdminTeam:Password before the first startup."),
    builder.Configuration.GetValue<bool>("SampleData:Enabled"));

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
