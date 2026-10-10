using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using URLBox.Domain.Interfaces;
using URLBox.Presentation.Authentication;
using URLBox.Presentation.Models;

namespace URLBox.Presentation.Controllers;

[AllowAnonymous]
public class AccountController(ITeamStore store) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return LoginModal(null, returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("team-login")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return LoginModal("Enter your team name and password.", model.ReturnUrl);
        var team = await store.AuthenticateAsync(model.TeamName, model.Password);
        if (team is null) return LoginModal("Invalid team name or password.", model.ReturnUrl);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            TeamCookieEvents.PrincipalFor(team), new AuthenticationProperties { IsPersistent = true });
        return !string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)
            ? LocalRedirect(model.ReturnUrl) : RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    private IActionResult LoginModal(string? error, string? returnUrl)
    {
        TempData["LoginError"] = error;
        TempData["ShowLoginModal"] = true;
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) TempData["LoginReturnUrl"] = returnUrl;
        return RedirectToAction("Index", "Home");
    }
}
