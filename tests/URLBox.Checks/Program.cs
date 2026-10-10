using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using URLBox.Domain.Entities;

using URLBox.Infrastructure.Storage;
using URLBox.Presentation.Authentication;
using URLBox.Presentation.Controllers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

var directory = Path.Combine(Path.GetTempPath(), "urlbox-checks-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
var checks = 0;
try
{
    var store = new YamlTeamStore(directory);
    await store.EnsureAdminAsync("admin", "admin1234", true);
    var teams = await store.GetTeamsAsync();
    Check(teams.Count == 2 && Directory.GetFiles(directory, "*.yaml").Length == 3, "teams.yaml plus a named URL file for each team");
    var admin = teams.Single(t => t.IsAdmin);
    var demo = teams.Single(t => !t.IsAdmin);
    Check(demo.Urls.Count == 3 && demo.Projects.Count == 2, "Sample URLs and projects");
    Check(demo.Projects.Select(p => p.Color).Distinct().Count() == 2
        && demo.Projects.All(p => ProjectColor.IsValid(p.Color)), "Existing projects receive distinct default colors");
    var yaml = await File.ReadAllTextAsync(Path.Combine(directory, demo.Name + ".yaml"));
    var legacyYaml = System.Text.RegularExpressions.Regex.Replace(yaml, @"(?m)^environments:\r?\n(?:- [^\r\n]*\r?\n)*", "");
    await File.WriteAllTextAsync(Path.Combine(directory, demo.Name + ".yaml"), legacyYaml);
    var legacyRead = await new YamlTeamStore(directory).GetAsync(demo.Id);
    Check(legacyRead!.Environments!.SequenceEqual(new[] { "Dev", "Stage", "Prod" }) && legacyRead.Urls.Count == 3,
        "Existing YAML without environments retains its URL environment names");
    await File.WriteAllTextAsync(Path.Combine(directory, demo.Name + ".yaml"), yaml);
    if (args.Length == 2 && args[0] == "--write-sample")
    {
        var sampleStore = new YamlTeamStore(args[1]);
        await sampleStore.EnsureAdminAsync("admin", "Admin#123", true);
        Check(await sampleStore.AuthenticateAsync("Profit", "profit1234") is not null
            && await sampleStore.AuthenticateAsync("admin", "Admin#123") is not null, "Repository sample logins work");
    }
    Check(yaml.Contains("urls:") && yaml.Contains("projects:") && !yaml.Contains("passwordHash") && !(await File.ReadAllTextAsync(Path.Combine(directory, "teams.yaml"))).Contains("profit1234"), "URL YAML excludes credentials; teams.yaml stores hashes");
    Check(await store.AuthenticateAsync(" PROFIT ", "profit1234") is not null && await store.AuthenticateAsync("Profit", "wrong") is null, "Shared team login and wrong-password rejection");
    await store.EnsureAdminAsync("admin", "changed-password", true);
    Check((await store.GetTeamsAsync()).Count == 2 && await store.AuthenticateAsync("admin", "admin1234") is not null, "Repeated startup does not overwrite existing data or credentials");
    var fresh = new YamlTeamStore(directory);
    Check((await fresh.GetAsync(demo.Id))!.Urls.Count == 3, "Team data survives store restart");
    var custom = await store.CreateAsync("تیم تست نمونه", "test1234");
    Check((await fresh.GetAsync(custom.Id))!.Environments!.Count == 0, "New teams start without predefined environments");
    await store.SaveEnvironmentAsync(custom.Id, "Stage");
    await store.SaveEnvironmentAsync(custom.Id, "Dev");
    await store.SaveEnvironmentAsync(custom.Id, "Beta custom");
    await Reject<InvalidOperationException>(() => store.SaveEnvironmentAsync(custom.Id, "stage"));
    await Reject<InvalidOperationException>(() => store.SaveEnvironmentAsync(custom.Id, " "));
    Check((await fresh.GetAsync(custom.Id))!.Environments!.SequenceEqual(new[] { "Stage", "Dev", "Beta custom" }),
        "Custom environments persist in creation order and reject duplicate or empty names");
    Check((await fresh.GetAsync(custom.Id))!.Name == custom.Name, "Persian and special characters round trip through YAML");
    await Reject<InvalidOperationException>(() => store.CreateAsync("Profit", "abcd"));
    Check(Directory.GetFiles(directory,"*.yaml").Length == 4, "Duplicate names do not create files");
    await Reject<InvalidOperationException>(() => store.GetAsync("../../outside"));
    await Reject<InvalidOperationException>(() => store.CreateAsync("../outside", "abcd"));
    await Reject<InvalidOperationException>(() => store.CreateAsync("teams", "abcd"));
    await Reject<InvalidOperationException>(() => store.CreateAsync("CON", "abcd"));
    checks++;
    var orphan = Path.Combine(directory, "Unregistered.yaml");
    await File.WriteAllTextAsync(orphan, "user-owned contents");
    await Reject<InvalidOperationException>(() => store.CreateAsync("Unregistered", "abcd"));
    Check(await File.ReadAllTextAsync(orphan) == "user-owned contents", "Creating a team cannot overwrite an unregistered file");
    await store.SaveProjectAsync(custom.Id, null, "Project: #1");
    await store.SaveUrlAsync(custom.Id, null, " https://example.com/?q=a&b=c ", " Unicode توضیح: #yes ", "Stage", new[] { "Project: #1", "project: #1" });
    var saved = (await fresh.GetAsync(custom.Id))!;
    Check(saved.Urls.Single().UrlValue == "https://example.com/?q=a&b=c" && saved.Urls.Single().ProjectIds.Count == 1, "URL details trim and project selection deduplicates");
    await store.SaveEnvironmentAsync(custom.Id, "QA تست", "Stage");
    Check((await fresh.GetAsync(custom.Id))!.Urls.Single().Environment == "QA تست", "Renaming an environment preserves its URLs and supports Unicode");
    await Reject<InvalidOperationException>(() => store.DeleteEnvironmentAsync(custom.Id, "QA تست"));
    await store.DeleteEnvironmentAsync(custom.Id, "Beta custom");
    Check(!(await fresh.GetAsync(custom.Id))!.Environments!.Contains("Beta custom"), "Unused environments can be deleted");
    await store.SaveProjectAsync(custom.Id, 1, "Renamed");
    await store.SaveProjectAsync(custom.Id, 1, "Renamed", "#abcdef");
    Check((await fresh.GetAsync(custom.Id))!.Projects.Single().Color == "#ABCDEF", "Project color persists across store instances");
    await store.SaveProjectAsync(custom.Id, 1, "Renamed");
    Check((await fresh.GetAsync(custom.Id))!.Projects.Single().Color == "#ABCDEF", "Renaming without a color preserves the saved color");
    await Reject<InvalidOperationException>(() => store.SaveProjectAsync(custom.Id, 1, "Renamed", "red;display:none"));
    Check((await fresh.GetAsync(custom.Id))!.Projects.Single().Color == "#ABCDEF", "Invalid project colors cannot change stored data");
    Check(ProjectColor.Foreground("#FFFFFF") == "#000000" && ProjectColor.Foreground("#000000") == "#FFFFFF", "Tag text contrasts with light and dark colors");
    Check((await fresh.GetAsync(custom.Id))!.Urls.Single().ProjectIds.Single() == 1, "Project rename preserves URL assignment");
    var original = await File.ReadAllTextAsync(Path.Combine(directory, custom.Name + ".yaml"));
    await Reject<InvalidOperationException>(() => store.SaveUrlAsync(custom.Id, 1, "javascript:alert(1)", "Unsafe", "Dev", new[] { "Renamed" }));
    await Reject<InvalidOperationException>(() => store.SaveUrlAsync(custom.Id, 1, "https://example.com", "Bad", "Missing", new[] { "Renamed" }));
    await Reject<InvalidOperationException>(() => store.SaveUrlAsync(custom.Id, 1, "https://example.com", "Bad", "Dev", new[] { "Other team's project" }));
    await Reject<InvalidOperationException>(() => store.DeleteProjectAsync(custom.Id, 1));
    Check(original == await File.ReadAllTextAsync(Path.Combine(directory, custom.Name + ".yaml")), "Invalid edits and project deletion do not change the file");
    var controller = ControllerFor(custom);
    Check(await controller.SaveEnvironment("Forbidden", teamId: demo.Id) is ForbidResult
        && await controller.DeleteEnvironment("Dev", demo.Id) is ForbidResult, "Environment changes cannot cross team boundaries");
    Check(await controller.SaveEnvironment("Preview") is RedirectToActionResult
        && (await fresh.GetAsync(custom.Id))!.Environments!.Contains("Preview"), "Teams can create their own environments");
    Check(await ControllerFor(admin).SaveEnvironment("Admin Preview", teamId: demo.Id) is RedirectToActionResult
        && (await fresh.GetAsync(demo.Id))!.Environments!.Contains("Admin Preview"), "Admins can manage selected team environments");
    Check(await controller.SaveProject("Renamed", 1, projectColor: "#000000") is RedirectToActionResult
        && (await fresh.GetAsync(custom.Id))!.Projects.Single().Color == "#000000", "Teams can configure their own project colors");
    var previousColor = (await fresh.GetAsync(demo.Id))!.Projects.Single(p => p.Id == 1).Color;
    Check(await controller.SaveProject("Documentation", 1, demo.Id, projectColor: "#000000") is ForbidResult
        && (await fresh.GetAsync(demo.Id))!.Projects.Single(p => p.Id == 1).Color == previousColor,
        "Teams cannot change another team's project colors");
    Check(await ControllerFor(admin).SaveProject("Documentation", 1, demo.Id, projectColor: "#C2410C") is RedirectToActionResult
        && (await fresh.GetAsync(demo.Id))!.Projects.Single(p => p.Id == 1).Color == "#C2410C", "Admin can set a selected team's project color");
    Check(await controller.EditUrl(1, "https://forbidden.example.com", "Forbidden", "Dev", new() { "Documentation" }, demo.Id) is ForbidResult, "Teams cannot edit another team's URLs");
    Check(await controller.DeleteUrl(1, demo.Id) is ForbidResult, "Teams cannot delete another team's URLs");
    Check(await controller.AddUrl("https://forbidden.example.com", "Forbidden", "Dev", new() { "Documentation" }, demo.Id) is ForbidResult, "Teams cannot add URLs to another team");
    Check(await controller.SaveProject("Forbidden", teamId: demo.Id) is ForbidResult && await controller.DeleteProject(1, demo.Id) is ForbidResult, "Project changes cannot cross team boundaries");
    Check((await store.GetAsync(demo.Id))!.Urls[0].Description == demo.Urls[0].Description, "Cross-team requests preserve target data");
    var result = await ControllerFor(admin).EditUrl(1, "https://updated.example.com", "Admin edit", "Prod", new() { "Documentation" }, demo.Id);
    Check(result is RedirectToActionResult && (await fresh.GetAsync(demo.Id))!.Urls[0].Description == "Admin edit", "Admin can edit a selected team's URLs");
    result = await ControllerFor(custom).EditUrl(1, "https://own.example.com", "Own edit", "Dev", new() { "Renamed" });
    Check(result is RedirectToActionResult && (await fresh.GetAsync(custom.Id))!.Urls[0].Description == "Own edit", "Teams can edit their own URLs");
    await Task.WhenAll(Enumerable.Range(0, 12).Select(i => (i % 2 == 0 ? store : fresh).SaveUrlAsync(custom.Id, null,
        "https://example.com/" + i, "Concurrent " + i, "Dev", new[] { "Renamed" })));
    saved = (await store.GetAsync(custom.Id))!;
    Check(saved.Urls.Count == 13 && saved.Urls.Select(u => u.Id).Distinct().Count() == 13, "Concurrent store instances retain every write with unique IDs");
    var oldName = custom.Name;
    await store.UpdateAsync(custom.Id, "Profit Local", null, false);
    custom.Name = "Profit Local";
    Check(File.Exists(Path.Combine(directory, custom.Name + ".yaml")) && !File.Exists(Path.Combine(directory, oldName + ".yaml"))
        && (await fresh.GetAsync(custom.Id))!.Urls.Count == 13, "Team rename moves its named URL file and preserves data");
    var session = saved.SessionVersion;
    await store.UpdateAsync(custom.Id, custom.Name, "new1234", false);
    Check(await store.AuthenticateAsync(custom.Name, "test1234") is null && await store.AuthenticateAsync(custom.Name, "new1234") is not null
        && (await store.GetAsync(custom.Id))!.SessionVersion != session, "Password changes invalidate old sessions and old password");
    await Reject<InvalidOperationException>(() => store.DeleteAsync(admin.Id));
    await Reject<InvalidOperationException>(() => store.UpdateAsync(admin.Id, admin.Name, null, false));
    checks++;
    await store.DeleteUrlAsync(custom.Id, 1);
    await store.SaveUrlAsync(custom.Id, null, "https://example.com/new", "New", "Dev", new[] { "Renamed" });
    Check((await store.GetAsync(custom.Id))!.Urls.Max(u => u.Id) == 14, "Deleted URL IDs are never reused");
    await store.DeleteAsync(custom.Id);
    Check(await store.GetAsync(custom.Id) is null && !File.Exists(Path.Combine(directory, custom.Name + ".yaml")), "Deleting a team removes only its own file");
    var legacyDirectory = Path.Combine(directory, "legacy");
    Directory.CreateDirectory(legacyDirectory);
    var legacyPath = Path.Combine(legacyDirectory, demo.Id + ".yaml");
    await File.WriteAllTextAsync(legacyPath, new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build().Serialize(demo));
    var legacyStore = new YamlTeamStore(legacyDirectory);
    Check((await legacyStore.GetTeamsAsync()).Single().Urls.Count == 3
        && File.Exists(Path.Combine(legacyDirectory, "teams.yaml")) && File.Exists(Path.Combine(legacyDirectory, "Profit.yaml"))
        && !File.Exists(legacyPath), "Previous YAML layout converts without losing URLs");
    Check(await legacyStore.AuthenticateAsync("Profit", "profit1234") is not null, "YAML conversion retains team credentials");
    var badPath = Path.Combine(directory, demo.Name + ".yaml");
    await File.WriteAllTextAsync(badPath, "name: [invalid YAML");
    await Reject<InvalidDataException>(() => store.SaveProjectAsync(demo.Id, null, "Cannot overwrite"));
    Check(await File.ReadAllTextAsync(badPath) == "name: [invalid YAML", "Malformed YAML is reported and never overwritten");
    Check(Directory.GetFiles(directory, "*.tmp").Length == 0, "Atomic writes leave no temporary files");
    var registryPath = Path.Combine(directory, "teams.yaml");
    await File.WriteAllTextAsync(registryPath, "teams: [invalid YAML");
    await Reject<InvalidDataException>(() => store.CreateAsync("No overwrite", "abcd"));
    Check(await File.ReadAllTextAsync(registryPath) == "teams: [invalid YAML", "Malformed registry is never overwritten");
    Console.WriteLine($"Passed {checks} YAML persistence, team access, CRUD, sample data, password and concurrency checks.");

    HomeController ControllerFor(Team team)
    {
        var context = new DefaultHttpContext { User = TeamCookieEvents.PrincipalFor(team) };
        return new HomeController(store)
        {
            ControllerContext = new ControllerContext { HttpContext = context },
            TempData = new TempDataDictionary(context, new MemoryTempDataProvider())
        };
    }
    void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        checks++;
    }
}
finally
{
    var expected = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
    if (Path.GetFullPath(directory).StartsWith(expected, StringComparison.OrdinalIgnoreCase)) Directory.Delete(directory, true);
}

static async Task Reject<T>(Func<Task> action) where T : Exception
{
    try { await action(); }
    catch (T) { return; }
    throw new Exception($"Expected {typeof(T).Name}.");
}

sealed class MemoryTempDataProvider : ITempDataProvider
{
    public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
    public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
}
