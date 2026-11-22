using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using URLBox.Domain.Interfaces;

namespace URLBox.Presentation.Controllers;

[Authorize]
public class TeamBackupController(ITeamStore store) : Controller
{
    private const int MaxBackupBytes = 5 * 1024 * 1024;

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Download()
    {
        var teamId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (teamId is null) return Forbid();
        var team = await store.GetAsync(teamId);
        if (team is null) return NotFound();
        return File(await store.CreateTeamBackupAsync(teamId), "application/yaml",
            $"{team.Name}-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.yaml");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<IActionResult> Restore(IFormFile? backupFile, bool replaceExistingData = false)
    {
        var teamId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (teamId is null) return Forbid();
        try
        {
            if (!ModelState.IsValid || !replaceExistingData)
                throw new InvalidOperationException("Confirm that you want to replace your team's current data.");
            if (backupFile is null || backupFile.Length == 0 || backupFile.Length > MaxBackupBytes
                || Path.GetExtension(backupFile.FileName).ToLowerInvariant() is not (".yaml" or ".yml"))
                throw new InvalidOperationException("Choose a team YAML backup up to 5 MB.");
            await using var stream = backupFile.OpenReadStream();
            using var reader = new StreamReader(stream, new UTF8Encoding(false, true));
            await store.RestoreTeamBackupAsync(teamId, await reader.ReadToEndAsync());
            TempData["StatusMessage"] = "Your team's backup was restored.";
            TempData["StatusMessageType"] = "success";
        }
        catch (Exception ex) when (ex is InvalidOperationException or DecoderFallbackException)
        {
            TempData["RestoreError"] = ex is DecoderFallbackException ? "Choose a valid YAML text file." : ex.Message;
        }
        return RedirectToAction("Index", "Home");
    }
}
