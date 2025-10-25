using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using URLBox.Application.Services;
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
        private readonly UrlService _urlService;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ILogger<AdminController> logger,
            UrlService urlService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _urlService = urlService;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard(string? statusMessage = null)
        {
            var users = await _userManager.Users
                .OrderBy(u => u.UserName)
                .ToListAsync();

            var roleEntities = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .ToListAsync();

            var model = new AdminDashboardViewModel
            {
                Users = new List<UserRoleViewModel>(),
                StatusMessage = statusMessage
            };

            var roleUsage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                model.Users.Add(new UserRoleViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName ?? user.Email ?? "(no name)",
                    Email = user.Email ?? string.Empty,
                    Roles = roles.OrderBy(r => r).ToList()
                });

                foreach (var role in roles)
                {
                    if (!roleUsage.TryAdd(role, 1))
                    {
                        roleUsage[role]++;
                    }
                }
            }

            foreach (var role in roleEntities)
            {
                var roleName = role.Name ?? string.Empty;
                roleUsage.TryGetValue(roleName, out var count);

                model.Roles.Add(new RoleSummaryViewModel
                {
                    RoleId = role.Id,
                    RoleName = roleName,
                    AssignedUserCount = count
                });
            }

            var urlStats = await _urlService.GetStatisticsAsync();
            model.TotalUsers = model.Users.Count;
            model.TotalRoles = model.Roles.Count;
            model.UrlStatistics = urlStats;

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
        public async Task<IActionResult> UpdateRole(UpdateRoleInputModel input)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Dashboard), new { statusMessage = CollectModelErrors() });
            }

            var role = await _roleManager.FindByIdAsync(input.RoleId);
            if (role is null)
            {
                return RedirectToAction(nameof(Dashboard), new { statusMessage = "Role not found." });
            }

            var newName = input.RoleName.Trim();
            if (!string.Equals(role.Name, newName, StringComparison.OrdinalIgnoreCase)
                && await _roleManager.RoleExistsAsync(newName))
            {
                return RedirectToAction(nameof(Dashboard), new { statusMessage = $"Role '{newName}' already exists." });
            }

            role.Name = newName;
            role.NormalizedName = newName.ToUpperInvariant();

            var updateResult = await _roleManager.UpdateAsync(role);
            if (!updateResult.Succeeded)
            {
                var error = string.Join(" ", updateResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Dashboard), new { statusMessage = error });
            }

            _logger.LogInformation("Role '{RoleName}' renamed by {Admin}", newName, User.Identity?.Name);
            return RedirectToAction(nameof(Dashboard), new { statusMessage = $"Role renamed to '{newName}'." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRole(string roleId)
        {
            if (string.IsNullOrWhiteSpace(roleId))
            {
                return RedirectToAction(nameof(Dashboard), new { statusMessage = "Role identifier is required." });
            }

            var role = await _roleManager.FindByIdAsync(roleId);
            if (role is null)
            {
                return RedirectToAction(nameof(Dashboard), new { statusMessage = "Role not found." });
            }

            var roleName = role.Name ?? string.Empty;

            var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
            foreach (var user in usersInRole)
            {
                var removal = await _userManager.RemoveFromRoleAsync(user, roleName);
                if (!removal.Succeeded)
                {
                    var error = string.Join(" ", removal.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Dashboard), new { statusMessage = error });
                }
            }

            var deleteResult = await _roleManager.DeleteAsync(role);
            if (!deleteResult.Succeeded)
            {
                var error = string.Join(" ", deleteResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Dashboard), new { statusMessage = error });
            }

            _logger.LogInformation("Role '{RoleName}' deleted by {Admin}", roleName, User.Identity?.Name);
            return RedirectToAction(nameof(Dashboard), new { statusMessage = $"Role '{roleName}' deleted." });
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserInputModel input)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Dashboard), new { statusMessage = CollectModelErrors() });
            }

            var user = new ApplicationUser
            {
                UserName = input.UserName.Trim(),
                Email = input.Email.Trim()
            };

            var result = await _userManager.CreateAsync(user, input.Password);
            if (!result.Succeeded)
            {
                var error = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Dashboard), new { statusMessage = error });
            }

            _logger.LogInformation("User '{UserName}' created by {Admin}", user.UserName, User.Identity?.Name);
            return RedirectToAction(nameof(Dashboard), new { statusMessage = $"User '{user.UserName}' created." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUser(UpdateUserInputModel input)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Dashboard), new { statusMessage = CollectModelErrors() });
            }

            var user = await _userManager.FindByIdAsync(input.UserId);
            if (user is null)
            {
                return RedirectToAction(nameof(Dashboard), new { statusMessage = "User not found." });
            }

            user.UserName = input.UserName.Trim();
            user.Email = input.Email.Trim();

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var error = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Dashboard), new { statusMessage = error });
            }

            _logger.LogInformation("User '{UserName}' updated by {Admin}", user.UserName, User.Identity?.Name);
            return RedirectToAction(nameof(Dashboard), new { statusMessage = $"User '{user.UserName}' updated." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return RedirectToAction(nameof(Dashboard), new { statusMessage = "User identifier is required." });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return RedirectToAction(nameof(Dashboard), new { statusMessage = "User not found." });
            }

            var currentUserId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(currentUserId) && string.Equals(user.Id, currentUserId, StringComparison.Ordinal))
            {
                return RedirectToAction(nameof(Dashboard), new { statusMessage = "You cannot delete the account you are currently using." });
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var error = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Dashboard), new { statusMessage = error });
            }

            _logger.LogInformation("User '{UserName}' deleted by {Admin}", user.UserName, User.Identity?.Name);
            return RedirectToAction(nameof(Dashboard), new { statusMessage = $"User '{user.UserName}' deleted." });
        }

        private string CollectModelErrors()
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToArray();

            return errors.Length == 0 ? "Please review the submitted values." : string.Join(" ", errors);
        }
    }
}
