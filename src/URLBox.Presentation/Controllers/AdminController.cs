using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using URLBox.Domain.Entities;
using URLBox.Presentation.Models;

namespace URLBox.Presentation.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ILogger<AdminController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard(string? statusMessage = null)
        {
            var users = await _userManager.Users.OrderBy(u => u.UserName).ToListAsync();
            var roleNames = await _roleManager.Roles
                .Select(r => r.Name!)
                .OrderBy(n => n)
                .ToListAsync();

            var model = new AdminDashboardViewModel
            {
                Users = new List<UserRoleViewModel>(),
                Roles = roleNames,
                StatusMessage = statusMessage
            };

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                model.Users.Add(new UserRoleViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName ?? user.Email ?? "(no name)",
                    Roles = roles.OrderBy(r => r).ToList()
                });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return RedirectToAction(nameof(Dashboard), new { statusMessage = "Role name cannot be empty." });
            }

            roleName = roleName.Trim();
            if (await _roleManager.RoleExistsAsync(roleName))
            {
                return RedirectToAction(nameof(Dashboard), new { statusMessage = $"Role '{roleName}' already exists." });
            }

            var result = await _roleManager.CreateAsync(new ApplicationRole { Name = roleName });
            if (!result.Succeeded)
            {
                var error = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Dashboard), new { statusMessage = error });
            }

            _logger.LogInformation("Role '{RoleName}' created by {Admin}", roleName, User.Identity?.Name);
            return RedirectToAction(nameof(Dashboard), new { statusMessage = $"Role '{roleName}' created." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return RedirectToAction(nameof(Dashboard), new { statusMessage = "User not found." });
            }

            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                return RedirectToAction(nameof(Dashboard), new { statusMessage = "Role not found." });
            }

            if (await _userManager.IsInRoleAsync(user, roleName))
            {
                return RedirectToAction(nameof(Dashboard), new { statusMessage = $"User already has role '{roleName}'." });
            }

            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (!result.Succeeded)
            {
                var error = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Dashboard), new { statusMessage = error });
            }

            _logger.LogInformation("Role '{RoleName}' assigned to user '{UserName}'", roleName, user.UserName);
            return RedirectToAction(nameof(Dashboard), new { statusMessage = $"Assigned '{roleName}' to {user.UserName}." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRole(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return RedirectToAction(nameof(Dashboard), new { statusMessage = "User not found." });
            }

            if (!await _userManager.IsInRoleAsync(user, roleName))
            {
                return RedirectToAction(nameof(Dashboard), new { statusMessage = $"User is not in role '{roleName}'." });
            }

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            if (!result.Succeeded)
            {
                var error = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Dashboard), new { statusMessage = error });
            }

            _logger.LogInformation("Role '{RoleName}' removed from user '{UserName}'", roleName, user.UserName);
            return RedirectToAction(nameof(Dashboard), new { statusMessage = $"Removed '{roleName}' from {user.UserName}." });
        }
    }
}
