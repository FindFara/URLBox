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

            if (!string.IsNullOrEmpty(returnUrl))
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
                TempData["LoginError"] = "Please provide both a username and password.";
                TempData["ShowLoginModal"] = true;
                TempData["LoginReturnUrl"] = returnUrl;
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user is null)
            {
                _logger.LogWarning("Login attempt failed for unknown user '{UserName}'", model.UserName);
                return HandleFailedLogin(returnUrl);
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
                TempData["LoginError"] = "Your account is locked. Please contact the administrator.";
                TempData["ShowLoginModal"] = true;
                TempData["LoginReturnUrl"] = returnUrl;
                return RedirectToAction("Index", "Home");
            }

            _logger.LogWarning("Invalid password for user '{UserName}'.", model.UserName);
            return HandleFailedLogin(returnUrl);
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

        private IActionResult HandleFailedLogin(string? returnUrl)
        {
            const string errorMessage = "Invalid username or password.";

            TempData["LoginError"] = errorMessage;
            TempData["ShowLoginModal"] = true;
            TempData["LoginReturnUrl"] = returnUrl ?? Url.Action("Index", "Home");
            return RedirectToAction("Index", "Home");
        }
    }
}
