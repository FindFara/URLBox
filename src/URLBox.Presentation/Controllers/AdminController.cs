using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using URLBox.Application.Services;
using URLBox.Application.ViewModel;
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
        private readonly ProjectService _projectService;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ILogger<AdminController> logger,
            UrlService urlService,
            ProjectService projectService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _urlService = urlService;
            _projectService = projectService;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard(string? statusMessage = null)
        {
            var totalUsers = await _userManager.Users.CountAsync();

            var roleEntities = await _roleManager.Roles
                .Include(r => r.Projects)
                .OrderBy(r => r.Name)
                .ToListAsync();

            var model = new AdminDashboardViewModel
            {
                Users = new List<UserRoleViewModel>(),
                StatusMessage = statusMessage
            };

            var roleUsage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var role in roleEntities)
            {
                var roleName = role.Name ?? string.Empty;
                if (string.IsNullOrWhiteSpace(roleName))
                {
                    continue;
                }

                var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
                roleUsage[roleName] = usersInRole.Count;
            }

            foreach (var role in roleEntities)
            {
                var roleName = role.Name ?? string.Empty;
                roleUsage.TryGetValue(roleName, out var count);

                model.Roles.Add(new RoleSummaryViewModel
                {
                    RoleId = role.Id,
                    RoleName = roleName,
                    AssignedUserCount = count,
                    AssignedProjects = role.Projects
                        .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                        .Select(p => new ProjectViewModel
                        {
                            Id = p.Id,
                            Name = p.Name
                        })
                        .ToList()
                });
            }

            var urlStats = await _urlService.GetStatisticsAsync();
            var projects = (await _projectService.GetProjectsAsync())
                .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            model.TotalUsers = totalUsers;
            model.TotalRoles = model.Roles.Count;
            model.UrlStatistics = urlStats;
            model.Projects = projects;

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> UserManager(string? statusMessage = null, int userPage = 1, int userPageSize = 10)
        {
            var model = await BuildUserManagerModel(statusMessage, userPage, userPageSize);
            return View(model);
        }

        [HttpGet]
        public Task<IActionResult> Users(string? statusMessage = null, int userPage = 1, int userPageSize = 10)
        {
            return UserManager(statusMessage, userPage, userPageSize);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProject(string projectName)
        {
            if (string.IsNullOrWhiteSpace(projectName))
            {
                return RedirectToAction(nameof(Dashboard), new { statusMessage = "Project name is required." });
            }

            var trimmedName = projectName.Trim();

            try
            {
                await _projectService.AddProjectAsync(trimmedName);
            }
            catch (InvalidOperationException ex)
            {
                return RedirectToAction(nameof(Dashboard), new { statusMessage = ex.Message });
            }

            _logger.LogInformation("Project '{ProjectName}' created by {Admin}", trimmedName, User.Identity?.Name);
            return RedirectToAction(nameof(Dashboard), new { statusMessage = $"Project '{trimmedName}' created." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProject(int projectId, string projectName)
        {
            if (string.IsNullOrWhiteSpace(projectName))
            {
                return RedirectToAction(nameof(Dashboard), new { statusMessage = "Project name is required." });
            }

            var trimmedName = projectName.Trim();

            try
            {
                await _projectService.UpdateProjectAsync(projectId, trimmedName);
            }
            catch (InvalidOperationException ex)
            {
                return RedirectToAction(nameof(Dashboard), new { statusMessage = ex.Message });
            }

            _logger.LogInformation("Project '{ProjectName}' updated by {Admin}", trimmedName, User.Identity?.Name);
            return RedirectToAction(nameof(Dashboard), new { statusMessage = $"Project renamed to '{trimmedName}'." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProject(int projectId)
        {
            try
            {
                await _projectService.DeleteProjectAsync(projectId);
            }
            catch (InvalidOperationException ex)
            {
                return RedirectToAction(nameof(Dashboard), new { statusMessage = ex.Message });
            }

            _logger.LogInformation("Project with id {ProjectId} deleted by {Admin}", projectId, User.Identity?.Name);
            return RedirectToAction(nameof(Dashboard), new { statusMessage = "Project deleted." });
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
                return RedirectToAction(nameof(UserManager), new { statusMessage = "User not found." });
            }

            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                return RedirectToAction(nameof(UserManager), new { statusMessage = "Role not found." });
            }

            if (await _userManager.IsInRoleAsync(user, roleName))
            {
                return RedirectToAction(nameof(UserManager), new { statusMessage = $"User already has role '{roleName}'." });
            }

            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (!result.Succeeded)
            {
                var error = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(UserManager), new { statusMessage = error });
            }

            _logger.LogInformation("Role '{RoleName}' assigned to user '{UserName}'", roleName, user.UserName);
            return RedirectToAction(nameof(UserManager), new { statusMessage = $"Assigned '{roleName}' to {user.UserName}." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRole(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return RedirectToAction(nameof(UserManager), new { statusMessage = "User not found." });
            }

            if (!await _userManager.IsInRoleAsync(user, roleName))
            {
                return RedirectToAction(nameof(UserManager), new { statusMessage = $"User is not in role '{roleName}'." });
            }

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            if (!result.Succeeded)
            {
                var error = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(UserManager), new { statusMessage = error });
            }

            _logger.LogInformation("Role '{RoleName}' removed from user '{UserName}'", roleName, user.UserName);
            return RedirectToAction(nameof(UserManager), new { statusMessage = $"Removed '{roleName}' from {user.UserName}." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserInputModel input)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(UserManager), new { statusMessage = CollectModelErrors() });
            }

            var trimmedEmail = string.IsNullOrWhiteSpace(input.Email)
                ? null
                : input.Email.Trim();

            var user = new ApplicationUser
            {
                UserName = input.UserName.Trim(),
                Email = trimmedEmail
            };

            var result = await _userManager.CreateAsync(user, input.Password);
            if (!result.Succeeded)
            {
                var error = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(UserManager), new { statusMessage = error });
            }

            _logger.LogInformation("User '{UserName}' created by {Admin}", user.UserName, User.Identity?.Name);
            return RedirectToAction(nameof(UserManager), new { statusMessage = $"User '{user.UserName}' created." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUser(UpdateUserInputModel input)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(UserManager), new { statusMessage = CollectModelErrors() });
            }

            var user = await _userManager.FindByIdAsync(input.UserId);
            if (user is null)
            {
                return RedirectToAction(nameof(UserManager), new { statusMessage = "User not found." });
            }

            user.UserName = input.UserName.Trim();
            user.Email = string.IsNullOrWhiteSpace(input.Email)
                ? null
                : input.Email.Trim();

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var error = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(UserManager), new { statusMessage = error });
            }

            var newPassword = string.IsNullOrWhiteSpace(input.NewPassword) ? null : input.NewPassword;
            if (newPassword is not null)
            {
                if (string.IsNullOrWhiteSpace(input.ConfirmPassword))
                {
                    return RedirectToAction(nameof(UserManager), new { statusMessage = "Password confirmation is required." });
                }

                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);
                if (!resetResult.Succeeded)
                {
                    var error = string.Join(" ", resetResult.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(UserManager), new { statusMessage = error });
                }
            }

            _logger.LogInformation("User '{UserName}' updated by {Admin}", user.UserName, User.Identity?.Name);
            return RedirectToAction(nameof(UserManager), new { statusMessage = $"User '{user.UserName}' updated." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return RedirectToAction(nameof(UserManager), new { statusMessage = "User identifier is required." });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return RedirectToAction(nameof(UserManager), new { statusMessage = "User not found." });
            }

            var currentUserId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(currentUserId) && string.Equals(user.Id, currentUserId, StringComparison.Ordinal))
            {
                return RedirectToAction(nameof(UserManager), new { statusMessage = "You cannot delete the account you are currently using." });
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var error = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(UserManager), new { statusMessage = error });
            }

            _logger.LogInformation("User '{UserName}' deleted by {Admin}", user.UserName, User.Identity?.Name);
            return RedirectToAction(nameof(UserManager), new { statusMessage = $"User '{user.UserName}' deleted." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignProjectToRole(int projectId, string roleId)
        {
            if (projectId <= 0 || string.IsNullOrWhiteSpace(roleId))
            {
                return RedirectToAction(nameof(Dashboard), new { statusMessage = "Project and role are required." });
            }

            try
            {
                await _projectService.AssignRoleToProjectAsync(projectId, roleId);
            }
            catch (InvalidOperationException ex)
            {
                return RedirectToAction(nameof(Dashboard), new { statusMessage = ex.Message });
            }

            var project = (await _projectService.GetProjectsAsync()).FirstOrDefault(p => p.Id == projectId);
            var role = await _roleManager.FindByIdAsync(roleId);
            var projectName = project?.Name ?? $"ID {projectId}";
            var roleName = role?.Name ?? roleId;

            _logger.LogInformation(
                "Project '{ProjectName}' assigned to role '{RoleName}' by {Admin}",
                projectName,
                roleName,
                User.Identity?.Name);

            return RedirectToAction(nameof(Dashboard), new
            {
                statusMessage = $"Assigned project '{projectName}' to role '{roleName}'."
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveProjectFromRole(int projectId, string roleId)
        {
            if (projectId <= 0 || string.IsNullOrWhiteSpace(roleId))
            {
                return RedirectToAction(nameof(Dashboard), new { statusMessage = "Project and role are required." });
            }

            try
            {
                await _projectService.RemoveRoleFromProjectAsync(projectId, roleId);
            }
            catch (InvalidOperationException ex)
            {
                return RedirectToAction(nameof(Dashboard), new { statusMessage = ex.Message });
            }

            var project = (await _projectService.GetProjectsAsync()).FirstOrDefault(p => p.Id == projectId);
            var role = await _roleManager.FindByIdAsync(roleId);
            var projectName = project?.Name ?? $"ID {projectId}";
            var roleName = role?.Name ?? roleId;

            _logger.LogInformation(
                "Project '{ProjectName}' removed from role '{RoleName}' by {Admin}",
                projectName,
                roleName,
                User.Identity?.Name);

            return RedirectToAction(nameof(Dashboard), new
            {
                statusMessage = $"Removed project '{projectName}' from role '{roleName}'."
            });
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

        private async Task<AdminUserManagerViewModel> BuildUserManagerModel(string? statusMessage, int userPage, int userPageSize)
        {
            if (userPageSize <= 0)
            {
                userPageSize = 10;
            }
            userPageSize = Math.Min(userPageSize, 50);

            var totalUsers = await _userManager.Users.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalUsers / (double)userPageSize));
            userPage = Math.Clamp(userPage, 1, totalPages);

            var users = await _userManager.Users
                .OrderBy(u => u.UserName)
                .Skip((userPage - 1) * userPageSize)
                .Take(userPageSize)
                .ToListAsync();

            var model = new AdminUserManagerViewModel
            {
                StatusMessage = statusMessage,
                UserPage = userPage,
                UserPageSize = userPageSize,
                UserTotalCount = totalUsers,
                UserTotalPages = totalPages
            };

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
            }

            var rolesList = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .ToListAsync();

            model.Roles = rolesList
                .Where(r => !string.IsNullOrWhiteSpace(r.Name))
                .Select(r => new RoleSummaryViewModel
                {
                    RoleId = r.Id,
                    RoleName = r.Name ?? string.Empty
                })
                .ToList();

            return model;
        }
    }
}
