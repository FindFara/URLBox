using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using URLBox.Application.Services;
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
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            UrlService urlService,
            ProjectService projectService,
            UserManager<ApplicationUser> userManager,
            ILogger<HomeController> logger)
        {
            _urlService = urlService;
            _projectService = projectService;
            _userManager = userManager;
            _logger = logger;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUrl(string url, string description, EnvironmentType environment, string project, bool isPublic = false)
        {
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(project))
            {
                SetStatusMessage("Please provide a URL, description, and project.", true);
                return RedirectToAction("Index");
            }

            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
                SetStatusMessage("You need to be signed in to add URLs.", true);
                return RedirectToAction("Index");
            }

            var normalizedProject = project.Trim();
            var isAdmin = User.IsInRole("Administrator");

            if (!isAdmin)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user is null)
                {
                    SetStatusMessage("Unable to determine your account.", true);
                    return RedirectToAction("Index");
                }

                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains(normalizedProject, StringComparer.OrdinalIgnoreCase))
                {
                    SetStatusMessage("You cannot add URLs to this project.", true);
                    return RedirectToAction("Index");
                }
            }

            try
            {
                await _urlService.AddUrlAsync(
                    url.Trim(),
                    description.Trim(),
                    environment,
                    normalizedProject,
                    currentUserId,
                    isPublic);

                SetStatusMessage("URL added successfully.", false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add URL for project {Project}", normalizedProject);
                SetStatusMessage("We couldn't add that URL. Please try again.", true);
            }

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProject(string projectName)
        {
            if (!string.IsNullOrWhiteSpace(projectName))
            {
                await _projectService.AddProjectAsync(projectName.Trim());
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUrl(int id)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
                SetStatusMessage("You need to sign in before you can delete URLs.", true);
                return RedirectToAction("Index");
            }

            var isAdmin = User.IsInRole("Administrator");
            var deleted = await _urlService.DeleteUrlAsync(id, currentUserId, isAdmin);

            if (deleted)
            {
                SetStatusMessage("URL removed.", false);
            }
            else
            {
                SetStatusMessage("You do not have permission to delete that URL.", true);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateVisibility(int id, bool isPublic)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
                SetStatusMessage("You need to sign in before updating visibility.", true);
                return RedirectToAction("Index");
            }

            var isAdmin = User.IsInRole("Administrator");
            var updated = await _urlService.UpdateVisibilityAsync(id, isPublic, currentUserId, isAdmin);

            if (updated)
            {
                SetStatusMessage(isPublic ? "URL is now public." : "URL is now private.", false);
            }
            else
            {
                SetStatusMessage("You do not have permission to change that URL.", true);
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
            var projects = (await _projectService.GetProjectsAsync()).ToList();
            IEnumerable<string>? allowedProjects = Array.Empty<string>();
            string? currentUserId = null;
            var includePublic = !isPublicPage;

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user is not null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    currentUserId = user.Id;
                    if (roles.Contains("Administrator", StringComparer.OrdinalIgnoreCase))
                    {
                        allowedProjects = null;
                    }
                    else
                    {
                        allowedProjects = roles;
                    }
                }
            }

            var urls = (await _urlService.GetUrlsAsync(allowedProjects, isPublicPage, includePublic, currentUserId)).ToList();

            if (allowedProjects is not null)
            {
                var allowedSet = new HashSet<string>(allowedProjects, StringComparer.OrdinalIgnoreCase);
                projects = projects.Where(p => allowedSet.Contains(p.Name)).ToList();
            }

            if (isPublicPage)
            {
                var publicProjects = new HashSet<string>(urls.Select(u => u.Tag).Where(n => !string.IsNullOrWhiteSpace(n)), StringComparer.OrdinalIgnoreCase);
                projects = projects.Where(p => publicProjects.Contains(p.Name)).ToList();
            }

            TempData.TryGetValue("LoginReturnUrl", out var loginReturnUrlObj);

            ViewBag.Projects = projects;
            ViewBag.LoginError = TempData["LoginError"];
            ViewBag.IsPublicPage = isPublicPage;
            ViewBag.ShowLoginModal = TempData.ContainsKey("ShowLoginModal");
            ViewBag.LoginReturnUrl = loginReturnUrlObj as string ?? Url.Action("Index", "Home");
            ViewBag.StatusMessage = TempData["StatusMessage"];
            ViewBag.StatusMessageType = TempData["StatusMessageType"] ?? "info";
            ViewBag.CurrentUserId = currentUserId;
            ViewData["Title"] = isPublicPage ? "Public URLs" : "URL dashboard";

            return View("Index", urls);
        }

        private void SetStatusMessage(string message, bool isError)
        {
            TempData["StatusMessage"] = message;
            TempData["StatusMessageType"] = isError ? "danger" : "success";
        }
    }
}
