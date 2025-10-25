using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using URLBox.Application.Services;
using URLBox.Domain.Entities;
using URLBox.Domain.Enums;
using URLBox.Domain.Models;
using System.Linq;

namespace URLBox.Presentation.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly UrlService _urlService;
        private readonly ProjectService _projectService;
        private readonly UserManager<ApplicationUser> _userManager;

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
        public async Task<IActionResult> AddUrl(string url, string description, EnvironmentType environment, string project, bool isPublic = false)
        {
            url = url?.Trim() ?? string.Empty;
            project = project?.Trim() ?? string.Empty;
            description = description?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(project))
            {
                TempData["IndexStatus"] = "Please provide a URL and select a project.";
                return RedirectToAction("Index");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return Challenge();
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (!IsAdministrator(roles) && !HasProjectAccess(roles, project))
            {
                TempData["IndexStatus"] = "You are not allowed to add URLs for that project.";
                return RedirectToAction("Index");
            }

            await _urlService.AddUrlAsync(url, description, environment, project, isPublic);
            TempData["IndexStatus"] = "URL added successfully.";
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

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUrl(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return Challenge();
            }

            var url = await _urlService.GetUrlAsync(id);
            if (url is null)
            {
                TempData["IndexStatus"] = "The selected URL could not be found.";
                return RedirectToAction("Index");
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (!IsAdministrator(roles) && !HasProjectAccess(roles, url.Project?.Name))
            {
                TempData["IndexStatus"] = "You are not allowed to delete that URL.";
                return RedirectToAction("Index");
            }

            await _urlService.DeleteUrlAsync(id);
            TempData["IndexStatus"] = "URL deleted.";
            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetUrlVisibility(int id, bool isPublic)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return Challenge();
            }

            var url = await _urlService.GetUrlAsync(id);
            if (url is null)
            {
                TempData["IndexStatus"] = "The selected URL could not be found.";
                return RedirectToAction("Index");
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (!IsAdministrator(roles) && !HasProjectAccess(roles, url.Project?.Name))
            {
                TempData["IndexStatus"] = "You are not allowed to update that URL.";
                return RedirectToAction("Index");
            }

            await _urlService.UpdateVisibilityAsync(id, isPublic);
            TempData["IndexStatus"] = isPublic ? "URL marked as public." : "URL marked as private.";
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
            var includePublicUrls = false;

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user is not null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    if (IsAdministrator(roles))
                    {
                        allowedProjects = null;
                    }
                    else
                    {
                        allowedProjects = roles;
                        includePublicUrls = true;
                    }
                }
            }

            var urls = isPublicPage
                ? (await _urlService.GetUrlsAsync(publicOnly: true)).ToList()
                : (await _urlService.GetUrlsAsync(allowedProjects, includePublicUrls)).ToList();

            if (allowedProjects is not null)
            {
                var allowedSet = new HashSet<string>(allowedProjects, StringComparer.OrdinalIgnoreCase);
                projects = projects.Where(p => allowedSet.Contains(p.Name)).ToList();
            }

            ViewBag.Projects = projects;
            ViewBag.LoginError = TempData["LoginError"];
            ViewBag.LoginReturnUrl = TempData["LoginReturnUrl"];
            ViewBag.ShowLoginModal = TempData["ShowLoginModal"];
            ViewBag.IndexStatus = TempData["IndexStatus"];
            ViewBag.IsPublicPage = isPublicPage;
            ViewData["Title"] = isPublicPage ? "Public URLs" : "URL dashboard";

            return View("Index", urls);
        }

        private static bool IsAdministrator(ICollection<string> roles)
            => roles.Any(role => string.Equals(role, "Administrator", StringComparison.OrdinalIgnoreCase));

        private static bool HasProjectAccess(IEnumerable<string> roles, string? projectName)
        {
            if (string.IsNullOrWhiteSpace(projectName))
            {
                return false;
            }

            return roles.Any(role => string.Equals(role, projectName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
