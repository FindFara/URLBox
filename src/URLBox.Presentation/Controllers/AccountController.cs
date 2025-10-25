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

            return View(new LoginViewModel
            {
                ReturnUrl = string.IsNullOrEmpty(returnUrl) ? Url.Action("Index", "Home") : returnUrl
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null, bool useModal = false)
        {
            returnUrl ??= model.ReturnUrl ?? Url.Action("Index", "Home");

            if (!ModelState.IsValid)
            {
                if (useModal)
                {
                    TempData["LoginError"] = "Please provide both a username and password.";
                    return RedirectToAction("Index", "Home");
                }

                model.ReturnUrl = returnUrl;
                return View(model);
            }

            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user is null)
            {
                _logger.LogWarning("Login attempt failed for unknown user '{UserName}'", model.UserName);
                return HandleFailedLogin(model, returnUrl, useModal);
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
                if (useModal)
                {
                    TempData["LoginError"] = "Your account is locked. Please contact the administrator.";
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError(string.Empty, "Your account is locked. Please contact the administrator.");
                model.ReturnUrl = returnUrl;
                return View(model);
            }

            _logger.LogWarning("Invalid password for user '{UserName}'.", model.UserName);
            return HandleFailedLogin(model, returnUrl, useModal);
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

        private IActionResult HandleFailedLogin(LoginViewModel model, string? returnUrl, bool useModal)
        {
            const string errorMessage = "Invalid username or password.";

            if (useModal)
            {
                TempData["LoginError"] = errorMessage;
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, errorMessage);
            model.ReturnUrl = returnUrl;
            return View("Login", model);
        }
    }
}
