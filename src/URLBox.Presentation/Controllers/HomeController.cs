using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using URLBox.Application.Services;
using URLBox.Application.ViewModel;
using URLBox.Domain.Authorization;
using URLBox.Domain.Entities;
using URLBox.Domain.Enums;
using URLBox.Domain.Models;

namespace URLBox.Presentation.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly UrlService _urlService;
        private readonly ProjectService _projectService;
        private readonly UserManager<ApplicationUser> _userManager;

        private const string StatusMessageKey = "StatusMessage";
        private const string StatusMessageTypeKey = "StatusMessageType";
        private const string LoginErrorKey = "LoginError";
        private const string ShowLoginModalKey = "ShowLoginModal";
        private const string LoginReturnUrlKey = "LoginReturnUrl";

        public HomeController(
            UrlService urlService,
            ProjectService projectService,
            UserManager<ApplicationUser> userManager)
        {
            _urlService = urlService;
            _projectService = projectService;
            _userManager = userManager;
        }

        [AllowAnonymous]
        public Task<IActionResult> Index()
        {
            return RenderIndexAsync(isPublicPage: false);
        }

        [AllowAnonymous]
        public Task<IActionResult> Public()
        {
            return RenderIndexAsync(isPublicPage: true);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUrl(string url, string description, EnvironmentType environment, List<string> projects, bool isPublic = false)
        {
            var access = await BuildUserAccessContextAsync();
            if (!access.IsAdmin && !access.IsManager)
            {
                SetStatusMessage("You do not have permission to add URLs.", "danger");
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(url)
                || string.IsNullOrWhiteSpace(description)
                || projects is null
                || !projects.Any(p => !string.IsNullOrWhiteSpace(p)))
            {
                SetStatusMessage("Please provide all required fields for the new URL.", "warning");
                return RedirectToAction("Index");
            }

            var trimmedUrl = url.Trim();
            var trimmedDescription = description.Trim();
            var selectedProjects = projects
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            IEnumerable<string>? allowedProjects = null;
            if (!access.IsAdmin)
            {
                allowedProjects = (await _projectService.GetProjectsForRolesAsync(access.NonSystemRoles))
                    .Select(p => p.Name)
                    .ToList();
            }

            try
            {
                await _urlService.AddUrlAsync(
                    trimmedUrl,
                    trimmedDescription,
                    environment,
                    selectedProjects,
                    isPublic,
                    access.UserId,
                    allowedProjects,
                    access.IsAdmin);

                SetStatusMessage("URL added successfully.", "success");
            }
            catch (UnauthorizedAccessException)
            {
                SetStatusMessage("You are not allowed to add URLs for that project.", "danger");
            }
            catch (InvalidOperationException ex)
            {
                SetStatusMessage(ex.Message, "danger");
            }

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProject(string projectName)
        {
            if (string.IsNullOrWhiteSpace(projectName))
            {
                SetStatusMessage("Project name is required.", "warning");
                return RedirectToAction("Index");
            }

            try
            {
                await _projectService.AddProjectAsync(projectName);
                SetStatusMessage("Project added successfully.", "success");
            }
            catch (InvalidOperationException ex)
            {
                SetStatusMessage(ex.Message, "warning");
            }

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProject(int projectId, string projectName)
        {
            if (string.IsNullOrWhiteSpace(projectName))
            {
                SetStatusMessage("Project name is required.", "warning");
                return RedirectToAction("Index");
            }

            try
            {
                await _projectService.UpdateProjectAsync(projectId, projectName);
                SetStatusMessage("Project updated successfully.", "success");
            }
            catch (InvalidOperationException ex)
            {
                SetStatusMessage(ex.Message, "warning");
            }

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProject(int projectId)
        {
            try
            {
                await _projectService.DeleteProjectAsync(projectId);
                SetStatusMessage("Project deleted successfully.", "success");
            }
            catch (InvalidOperationException ex)
            {
                SetStatusMessage(ex.Message, "warning");
            }

            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUrl(int id)
        {
            var access = await BuildUserAccessContextAsync();
            if (!access.IsAdmin && !access.IsManager)
            {
                SetStatusMessage("You do not have permission to delete URLs.", "danger");
                return RedirectToAction("Index");
            }

            IEnumerable<string>? allowedProjects = null;
            if (!access.IsAdmin)
            {
                allowedProjects = (await _projectService.GetProjectsForRolesAsync(access.NonSystemRoles))
                    .Select(p => p.Name)
                    .ToList();
            }

            try
            {
                await _urlService.DeleteUrlAsync(id, allowedProjects, access.UserId, access.IsAdmin);
                SetStatusMessage("URL deleted successfully.", "success");
            }
            catch (UnauthorizedAccessException)
            {
                SetStatusMessage("You are not allowed to delete this URL.", "danger");
            }
            catch (InvalidOperationException ex)
            {
                SetStatusMessage(ex.Message, "warning");
            }

            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private async Task<IActionResult> RenderIndexAsync(bool isPublicPage)
        {
            var access = await BuildUserAccessContextAsync();
            var includeOnlyPublic = isPublicPage || !access.IsAuthenticated;
            var allProjects = (await _projectService.GetProjectsAsync()).ToList();
            var accessibleProjects = access.IsAdmin
                ? allProjects
                : (await _projectService.GetProjectsForRolesAsync(access.NonSystemRoles)).ToList();

            IEnumerable<string>? allowedProjects = access.IsAdmin
                ? null
                : accessibleProjects.Select(p => p.Name).ToList();

            var urls = (await _urlService.GetUrlsAsync(allowedProjects, access.UserId, includeOnlyPublic, access.IsAdmin)).ToList();

            List<ProjectViewModel> projects;
            if (includeOnlyPublic)
            {
                var visibleProjects = urls
                    .SelectMany(u => u.ProjectTags)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                projects = allProjects
                    .Where(p => visibleProjects.Contains(p.Name))
                    .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            else if (access.IsAdmin)
            {
                projects = allProjects;
            }
            else
            {
                projects = accessibleProjects.ToList();
            }

            var manageableProjects = isPublicPage
                ? Enumerable.Empty<ProjectViewModel>()
                : (access.IsAdmin ? allProjects : accessibleProjects);

            ViewBag.Projects = projects;
            ViewBag.ManageableProjects = manageableProjects;
            var hasManageableProjects = manageableProjects.Any();
            ViewBag.CanManageUrls = !isPublicPage
                && access.IsAuthenticated
                && (access.IsAdmin || (access.IsManager && hasManageableProjects));

            var statusMessage = TempData[StatusMessageKey] as string;
            if (!string.IsNullOrEmpty(statusMessage))
            {
                ViewBag.StatusMessage = statusMessage;
                ViewBag.StatusMessageType = TempData[StatusMessageTypeKey] as string ?? "info";
            }

            ViewBag.LoginError = TempData[LoginErrorKey];
            ViewBag.ShowLoginModal = TempData[ShowLoginModalKey];
            var loginReturnUrl = TempData[LoginReturnUrlKey] as string;
            if (!string.IsNullOrEmpty(loginReturnUrl))
            {
                ViewBag.LoginReturnUrl = loginReturnUrl;
            }

            ViewBag.IsPublicPage = isPublicPage;
            ViewData["Title"] = isPublicPage ? "Public URLs" : "URL dashboard";

            return View("Index", urls);
        }

        private async Task<UserAccessContext> BuildUserAccessContextAsync()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user is not null)
                {
                    var roles = (await _userManager.GetRolesAsync(user)).ToList();
                    var isAdmin = roles.Contains(AppRoles.Administrator, StringComparer.OrdinalIgnoreCase);
                    var isManager = isAdmin || roles.Contains(AppRoles.Manager, StringComparer.OrdinalIgnoreCase);
                    return new UserAccessContext(true, isAdmin, isManager, user.Id, roles);
                }
            }

            return UserAccessContext.Anonymous;
        }

        private void SetStatusMessage(string message, string type)
        {
            TempData[StatusMessageKey] = message;
            TempData[StatusMessageTypeKey] = type;
        }

        private sealed record UserAccessContext(bool IsAuthenticated, bool IsAdmin, bool IsManager, string? UserId, IReadOnlyCollection<string> Roles)
        {
            public IEnumerable<string> NonSystemRoles => Roles.Where(role => !AppRoles.IsSystemRole(role));

            public static UserAccessContext Anonymous { get; } = new(false, false, false, null, Array.Empty<string>());
        }
    }
}
