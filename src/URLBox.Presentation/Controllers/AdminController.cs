using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using URLBox.Application;
using URLBox.Application.ViewModel;
using URLBox.Domain.Authorization;
using URLBox.Domain.Interfaces;
using URLBox.Presentation.Models;

namespace URLBox.Presentation.Controllers;

[Authorize(Roles = AppRoles.Administrator)]
public class AdminController(ITeamStore store) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Dashboard(string? teamId = null, string urlSort = "description", bool descending = false)
    {
        var teams = await store.GetTeamsAsync();
        var selected = teams.FirstOrDefault(t => t.Id == (teamId ?? User.FindFirstValue(ClaimTypes.NameIdentifier)));
        if (teamId is not null && selected is null) return NotFound();
        var model = new AdminDashboardViewModel
        {
            Teams = teams,
            SelectedTeam = selected,
            Projects = selected?.ToProjects() ?? new(),
            TotalUrls = teams.Sum(t => t.Urls.Count),
            StatusMessage = TempData["StatusMessage"] as string,
            UrlSort = urlSort is "url" or "environment" or "project" ? urlSort : "description",
            Descending = descending
        };
        Func<UrlViewModel, string> key = model.UrlSort switch
        {
            "url" => u => u.UrlValue,
            "environment" => u => u.Environment.ToString(),
            "project" => u => string.Join(", ", u.ProjectTags),
            _ => u => u.Description
        };
        var urls = selected?.ToUrls() ?? new();
        model.Urls = (descending ? urls.OrderByDescending(key, StringComparer.OrdinalIgnoreCase)
            : urls.OrderBy(key, StringComparer.OrdinalIgnoreCase)).ThenBy(u => u.Id).ToList();
        ViewBag.TeamId = selected?.Id;
        ViewBag.Environments = selected?.Environments ?? new List<string>();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> TeamManager()
    {
        ViewBag.StatusMessage = TempData["StatusMessage"];
        return View(await store.GetTeamsAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTeam(TeamInputModel input)
    {
        try
        {
            if (!ModelState.IsValid) throw new InvalidOperationException("Enter a team name and a password of 4 to 100 characters.");
            if (string.IsNullOrEmpty(input.Id))
                await store.CreateAsync(input.Name, input.Password ?? string.Empty, input.IsAdmin);
            else
                await store.UpdateAsync(input.Id, input.Name, string.IsNullOrEmpty(input.Password) ? null : input.Password, input.IsAdmin);
            TempData["StatusMessage"] = "Team saved.";
        }
        catch (InvalidOperationException ex) { TempData["StatusMessage"] = ex.Message; }
        return RedirectToAction(nameof(TeamManager));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTeam(string id)
    {
        try
        {
            if (id == User.FindFirstValue(ClaimTypes.NameIdentifier))
                throw new InvalidOperationException("You cannot delete the team you are currently using.");
            await store.DeleteAsync(id);
            TempData["StatusMessage"] = "Team and its URLs deleted.";
        }
        catch (InvalidOperationException ex) { TempData["StatusMessage"] = ex.Message; }
        return RedirectToAction(nameof(TeamManager));
    }
}
