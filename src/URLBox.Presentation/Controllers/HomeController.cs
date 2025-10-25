using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using URLBox.Application.Services;
using URLBox.Application.ViewModel;
using URLBox.Domain.Enums;
using URLBox.Domain.Models;

namespace URLBox.Presentation.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UrlService _urlService;
        private readonly ProjectService _projectService;

        public HomeController(
            ILogger<HomeController> logger,
            UrlService urlService,
            ProjectService projectService)
        {
            _logger = logger;
            _urlService = urlService;
            _projectService = projectService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var projects = (await _projectService.GetProjectsAsync()).ToList();
            IEnumerable<UrlViewModel> urls = new List<UrlViewModel>();

            if (User.Identity?.IsAuthenticated ?? false)
            {
                if (User.IsInRole("Admin"))
                {
                    urls = await _urlService.GetUrlsAsync();
                }
                else
                {
                    var allowedProjects = User.Claims
                        .Where(c => c.Type == ClaimTypes.Role && !string.Equals(c.Value, "Admin", StringComparison.OrdinalIgnoreCase))
                        .Select(c => c.Value)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    urls = await _urlService.GetUrlsForProjectsAsync(allowedProjects);
                    projects = projects
                        .Where(p => allowedProjects.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                        .ToList();
                }
            }

            ViewBag.Projects = projects;
            ViewBag.HasAccess = User.Identity?.IsAuthenticated ?? false;
            return View(urls.ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUrl(string url, string description, EnvironmentType environment, string project)
        {
            if (!string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(project))
            {
                await _urlService.AddUrlAsync(url, description, environment, project);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProject(string projectName)
        {
            if (!string.IsNullOrWhiteSpace(projectName))
            {
                await _projectService.AddProjectAsync(projectName);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUrl(int id)
        {
            await _urlService.DeleteUrlAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
