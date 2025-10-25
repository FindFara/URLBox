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

        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUrl(string url, string description, EnvironmentType environment, string project)
        {
            if (!string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(project))
            {
                await _urlService.AddUrlAsync(url, description, environment, project);
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

        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUrl(int id)
        {
            await _urlService.DeleteUrlAsync(id);
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

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user is not null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
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

            var urls = (await _urlService.GetUrlsAsync(allowedProjects)).ToList();

            if (allowedProjects is not null)
            {
                var allowedSet = new HashSet<string>(allowedProjects, StringComparer.OrdinalIgnoreCase);
                projects = projects.Where(p => allowedSet.Contains(p.Name)).ToList();
            }

            ViewBag.Projects = projects;
            ViewBag.LoginError = TempData["LoginError"];
            ViewBag.IsPublicPage = isPublicPage;
            ViewData["Title"] = isPublicPage ? "Public URLs" : "URL dashboard";

            return View("Index", urls);
        }
    }
}
