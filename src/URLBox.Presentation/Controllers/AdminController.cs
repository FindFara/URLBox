using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using URLBox.Domain.Entities;
using URLBox.Presentation.Models.Admin;

namespace URLBox.Presentation.Controllers
{
    [Authorize(Roles = "Admin")]
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
        public async Task<IActionResult> Dashboard()
        {
            var users = await _userManager.Users.ToListAsync();
            var roles = await _roleManager.Roles.ToListAsync();

            var roleDistribution = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var role in roles)
            {
                if (!string.IsNullOrWhiteSpace(role.Name))
                {
                    roleDistribution[role.Name] = 0;
                }
            }

            var unassignedCount = 0;
            foreach (var user in users)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                if (userRoles.Count == 0)
                {
                    unassignedCount++;
                    continue;
                }

                foreach (var role in userRoles)
                {
                    if (!string.IsNullOrWhiteSpace(role))
                    {
                        roleDistribution.TryGetValue(role, out var count);
                        roleDistribution[role] = count + 1;
                    }
                }
            }

            if (unassignedCount > 0)
            {
                roleDistribution["Unassigned"] = unassignedCount;
            }

            var model = new AdminDashboardViewModel
            {
                TotalUsers = users.Count,
                TotalRoles = roles.Count,
                RoleDistribution = roleDistribution
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ManageUsers()
        {
            var users = await _userManager.Users
                .OrderBy(u => u.UserName)
                .ToListAsync();

            var userModels = new List<UserListItemViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userModels.Add(new UserListItemViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Roles = roles.ToArray()
                });
            }

            var roleNames = await _roleManager.Roles
                .Select(r => r.Name!)
                .Where(name => name != null)
                .OrderBy(name => name)
                .ToListAsync();

            var model = new ManageUsersViewModel
            {
                Users = userModels,
                AvailableRoles = roleNames,
                AssignRole = new AssignRoleInputModel()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(AssignRoleInputModel input)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(ManageUsers));
            }

            var user = await _userManager.FindByIdAsync(input.UserId);
            if (user is null)
            {
                TempData["AdminError"] = "User not found.";
                return RedirectToAction(nameof(ManageUsers));
            }

            var roleName = input.RoleName.Trim();
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                TempData["AdminError"] = $"Role '{roleName}' does not exist.";
                return RedirectToAction(nameof(ManageUsers));
            }

            if (await _userManager.IsInRoleAsync(user, roleName))
            {
                TempData["AdminInfo"] = $"User is already in role '{roleName}'.";
                return RedirectToAction(nameof(ManageUsers));
            }

            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (!result.Succeeded)
            {
                TempData["AdminError"] = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(ManageUsers));
            }

            TempData["AdminSuccess"] = $"Role '{roleName}' assigned to {user.UserName}.";
            _logger.LogInformation("Role {Role} assigned to user {User}", roleName, user.UserName);
            return RedirectToAction(nameof(ManageUsers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRole(string userId, string roleName)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(roleName))
            {
                return RedirectToAction(nameof(ManageUsers));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                TempData["AdminError"] = "User not found.";
                return RedirectToAction(nameof(ManageUsers));
            }

            if (!await _userManager.IsInRoleAsync(user, roleName))
            {
                TempData["AdminInfo"] = $"User is not in role '{roleName}'.";
                return RedirectToAction(nameof(ManageUsers));
            }

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            if (!result.Succeeded)
            {
                TempData["AdminError"] = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(ManageUsers));
            }

            TempData["AdminSuccess"] = $"Role '{roleName}' removed from {user.UserName}.";
            _logger.LogInformation("Role {Role} removed from user {User}", roleName, user.UserName);
            return RedirectToAction(nameof(ManageUsers));
        }

        [HttpGet]
        public async Task<IActionResult> ManageRoles()
        {
            var roleNames = await _roleManager.Roles
                .Select(r => r.Name!)
                .Where(name => name != null)
                .OrderBy(name => name)
                .ToListAsync();

            var model = new ManageRolesViewModel
            {
                Roles = roleNames,
                CreateRole = new CreateRoleInputModel()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(CreateRoleInputModel input)
        {
            if (!ModelState.IsValid)
            {
                TempData["AdminError"] = "Role name is required.";
                return RedirectToAction(nameof(ManageRoles));
            }

            var roleName = input.RoleName.Trim();
            if (await _roleManager.RoleExistsAsync(roleName))
            {
                TempData["AdminInfo"] = $"Role '{roleName}' already exists.";
                return RedirectToAction(nameof(ManageRoles));
            }

            var result = await _roleManager.CreateAsync(new ApplicationRole { Name = roleName });
            if (!result.Succeeded)
            {
                TempData["AdminError"] = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(ManageRoles));
            }

            TempData["AdminSuccess"] = $"Role '{roleName}' created.";
            _logger.LogInformation("Role {Role} created", roleName);
            return RedirectToAction(nameof(ManageRoles));
        }
    }
}
