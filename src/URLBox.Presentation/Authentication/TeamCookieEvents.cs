using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using URLBox.Domain.Authorization;
using URLBox.Domain.Entities;
using URLBox.Domain.Interfaces;

namespace URLBox.Presentation.Authentication;

public sealed class TeamCookieEvents(ITeamStore store) : CookieAuthenticationEvents
{
    public static ClaimsPrincipal PrincipalFor(Team team)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, team.Id),
            new(ClaimTypes.Name, team.Name),
            new(TeamAccount.SessionVersionClaim, team.SessionVersion)
        };
        if (team.IsAdmin) claims.Add(new Claim(ClaimTypes.Role, AppRoles.Administrator));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var version = context.Principal?.FindFirstValue(TeamAccount.SessionVersionClaim);
        var team = Guid.TryParseExact(id, "N", out _) ? await store.GetAsync(id!) : null;
        if (team is null || team.SessionVersion != version)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }
        if (context.Principal!.Identity?.Name != team.Name
            || context.Principal.IsInRole(AppRoles.Administrator) != team.IsAdmin)
        {
            context.ReplacePrincipal(PrincipalFor(team));
            context.ShouldRenew = true;
        }
    }
}
