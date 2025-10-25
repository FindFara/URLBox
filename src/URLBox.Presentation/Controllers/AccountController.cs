using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using URLBox.Domain.Entities;
using URLBox.Presentation.Models;

namespace URLBox.Presentation.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                TempData["LoginReturnUrl"] = returnUrl;
            }

            TempData["ShowLoginModal"] = true;
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            returnUrl ??= model.ReturnUrl ?? Url.Action("Index", "Home");

            if (!ModelState.IsValid)
            {
                return RedirectWithLoginError("Please provide both a username and password.", returnUrl);
            }

            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user is null)
            {
                _logger.LogWarning("Login attempt failed for unknown user '{UserName}'", model.UserName);
                return RedirectWithLoginError("Invalid username or password.", returnUrl);
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                _logger.LogInformation("User '{UserName}' logged in successfully.", model.UserName);
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User '{UserName}' account locked out.", model.UserName);
                return RedirectWithLoginError("Your account is locked. Please contact the administrator.", returnUrl);
            }

            _logger.LogWarning("Invalid password for user '{UserName}'.", model.UserName);
            return RedirectWithLoginError("Invalid username or password.", returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                await _signInManager.SignOutAsync();
                _logger.LogInformation("User logged out.");
            }

            return RedirectToAction("Index", "Home");
        }

        private IActionResult RedirectWithLoginError(string message, string? returnUrl)
        {
            TempData["LoginError"] = message;
            TempData["ShowLoginModal"] = true;

            var normalizedReturnUrl = NormalizeReturnUrl(returnUrl);
            TempData["LoginReturnUrl"] = normalizedReturnUrl;

            return RedirectToAction("Index", "Home");
        }

        private string NormalizeReturnUrl(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return returnUrl;
            }

            return Url.Action("Index", "Home") ?? "/";
        }
    }
}
