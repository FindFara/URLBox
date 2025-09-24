using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using URLBox.Application.Services;
using URLBox.Application.ViewModel;
using URLBox.Domain.Enums;
using URLBox.Domain.Models;

namespace URLBox.Presentation.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UrlService _urlService;
        private readonly ProjectService _projectService;
        private readonly RoleManager<IdentityRole> _roleManager;


        public HomeController(ILogger<HomeController> logger, UrlService urlService, ProjectService projectService, RoleManager<IdentityRole> roleManager)
        {
            _logger = logger;
            _urlService = urlService;
            _projectService = projectService;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var urls = await _urlService.GetUrlsAsync();
            var projects = await _projectService.GetProjectsAsync();
            var rols =  _roleManager.Roles.ToList();

            ViewBag.Projects = projects;
            ViewBag.teams = rols;
            return View(urls.ToList());
        }

        [HttpPost]
        public async Task<IActionResult> AddUrl(string url, string description, EnvironmentType environment, string tag)
        {
            if (!string.IsNullOrEmpty(url))
            {
                await _urlService.AddUrlAsync(url, description, environment, tag);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> AddProject(string projectName)
        {
            if (!string.IsNullOrWhiteSpace(projectName))
            {
                await _projectService.AddProjectAsync(projectName);
            }
            return RedirectToAction("Index");
        }


        public async Task<IActionResult> DeleteUrl(int id)
        {
            await _urlService.DeleteUrlAsync(id);
            return RedirectToAction("Index");
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
