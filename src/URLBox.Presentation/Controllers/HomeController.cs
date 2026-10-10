using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using URLBox.Application;
using URLBox.Application.ViewModel;
using URLBox.Domain.Authorization;
using URLBox.Domain.Interfaces;
using URLBox.Domain.Models;

namespace URLBox.Presentation.Controllers;

[Authorize]
public class HomeController(ITeamStore store) : Controller
{
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var teamId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var team = teamId is not null ? await store.GetAsync(teamId) : null;
        var projects = team?.ToProjects() ?? new List<ProjectViewModel>();
        ViewBag.TeamId = team?.Id;
        ViewBag.TeamName = team?.Name;
        ViewBag.Projects = projects;
        ViewBag.Environments = team?.Environments ?? new List<string>();
        ViewBag.AssignableProjects = projects;
        ViewBag.ManageableProjects = projects;
        ViewBag.CanManageUrls = team is not null;
        ViewBag.StatusMessage = TempData["StatusMessage"];
        ViewBag.StatusMessageType = TempData["StatusMessageType"];
        ViewBag.LoginError = TempData["LoginError"];
        ViewBag.ShowLoginModal = TempData["ShowLoginModal"];
        ViewBag.LoginReturnUrl = TempData["LoginReturnUrl"];
        return View(team?.ToUrls() ?? new List<UrlViewModel>());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> AddUrl(string url, string description, string environment,
        List<string> projects, string? teamId = null, string? returnUrl = null) =>
        Change(teamId, returnUrl, id => store.SaveUrlAsync(id, null, url, description, environment, projects ?? new()), "URL added.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> EditUrl(int id, string url, string description, string environment,
        List<string> projects, string? teamId = null, string? returnUrl = null) =>
        Change(teamId, returnUrl, target => store.SaveUrlAsync(target, id, url, description, environment, projects ?? new()), "URL updated.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> DeleteUrl(int id, string? teamId = null, string? returnUrl = null) =>
        Change(teamId, returnUrl, target => store.DeleteUrlAsync(target, id), "URL deleted.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> SaveProject(string projectName, int? projectId = null, string? teamId = null, string? returnUrl = null, string? projectColor = null)
    {
        return Change(teamId, returnUrl, target => store.SaveProjectAsync(target, projectId, projectName, projectColor), "Project saved.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> DeleteProject(int projectId, string? teamId = null, string? returnUrl = null) =>
        Change(teamId, returnUrl, target => store.DeleteProjectAsync(target, projectId), "Project deleted.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> SaveEnvironment(string environmentName, string? originalName = null, string? teamId = null, string? returnUrl = null) =>
        Change(teamId, returnUrl, target => store.SaveEnvironmentAsync(target, environmentName, originalName), "Environment saved.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> DeleteEnvironment(string environmentName, string? teamId = null, string? returnUrl = null) =>
        Change(teamId, returnUrl, target => store.DeleteEnvironmentAsync(target, environmentName), "Environment deleted.");

    private async Task<IActionResult> Change(string? teamId, string? returnUrl, Func<string, Task> operation, string message)
    {
        var current = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var target = string.IsNullOrEmpty(teamId) ? current : teamId;
        if (current is null || target is null || (target != current && !User.IsInRole(AppRoles.Administrator)))
            return Forbid();
        try
        {
            if (!ModelState.IsValid) throw new InvalidOperationException("Please review the submitted values.");
            await operation(target);
            TempData["StatusMessage"] = message;
            TempData["StatusMessageType"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["StatusMessage"] = ex.Message;
            TempData["StatusMessageType"] = "danger";
        }
        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Index));
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
