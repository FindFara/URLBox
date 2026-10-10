using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using URLBox.Domain.Entities;
using URLBox.Domain.Interfaces;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace URLBox.Infrastructure.Storage;

public sealed class YamlTeamStore : ITeamStore
{
    private readonly string _directory;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly PasswordHasher<Team> _hasher = new();
    private readonly ISerializer _serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance).WithDuplicateKeyChecking().Build();

    public YamlTeamStore(string directory)
    {
        _directory = Path.GetFullPath(directory);
        Directory.CreateDirectory(_directory);
    }

    public Task<List<Team>> GetTeamsAsync() => LockedAsync(ReadAllAsync);

    public Task<Team?> GetAsync(string id) => LockedAsync(async () =>
    {
        if (!Guid.TryParseExact(id, "N", out _)) throw new InvalidOperationException("Invalid team identifier.");
        return (await ReadAllAsync()).SingleOrDefault(t => t.Id == id);
    });

    public Task<Team?> AuthenticateAsync(string name, string password) => LockedAsync(async () =>
    {
        var team = (await ReadAllAsync()).SingleOrDefault(t => t.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
        if (team is null || string.IsNullOrEmpty(password)) return null;
        var result = _hasher.VerifyHashedPassword(team, team.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed) return null;
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            team.PasswordHash = _hasher.HashPassword(team, password);
            await WriteAsync(team);
        }
        return team;
    });

    public Task EnsureAdminAsync(string name, string password, bool seedSampleData = false) => LockedAsync(async () =>
    {
        var teams = await ReadAllAsync();
        if (teams.Any(t => t.IsAdmin)) return;
        // Never overwrite an existing team's credentials to bootstrap an administrator.
        var addSamples = seedSampleData && teams.Count == 0;
        var admin = await CreateCoreAsync(teams, name, password, true);
        if (addSamples)
        {
            admin.Projects.Add(new TeamProject { Id = admin.NextProjectId++, Name = "URLBox" });
            admin.Urls.Add(new TeamUrl
            {
                Id = admin.NextUrlId++, UrlValue = "https://github.com/FindFara/URLBox",
                Description = "URLBox repository", Environment = "Dev", ProjectIds = new() { 1 }
            });
            admin.Environments = new() { "Dev" };
            await WriteAsync(admin);
            var demoName = name.Equals("Profit", StringComparison.OrdinalIgnoreCase) ? "Profit Sample" : "Profit";
            var demo = await CreateCoreAsync(new() { admin }, demoName, "profit1234", false);
            demo.Projects = new() { new() { Id = 1, Name = "Documentation" }, new() { Id = 2, Name = "API" } };
            demo.NextProjectId = 3;
            demo.Environments = new() { "Dev", "Stage", "Prod" };
            demo.Urls = new()
            {
                new() { Id = 1, UrlValue = "https://learn.microsoft.com/aspnet/core/", Description = "ASP.NET Core documentation", Environment = "Dev", ProjectIds = new() { 1 } },
                new() { Id = 2, UrlValue = "https://example.com/staging/api", Description = "Sample staging API", Environment = "Stage", ProjectIds = new() { 2 } },
                new() { Id = 3, UrlValue = "https://example.com/production/api", Description = "Sample production API", Environment = "Prod", ProjectIds = new() { 2 } }
            };
            demo.NextUrlId = 4;
            await WriteAsync(demo);
        }
    });

    public Task<Team> CreateAsync(string name, string password, bool isAdmin = false) => LockedAsync(async () =>
        await CreateCoreAsync(await ReadAllAsync(), name, password, isAdmin));

    public Task UpdateAsync(string id, string name, string? password, bool isAdmin) => LockedAsync(async () =>
    {
        var teams = await ReadAllAsync();
        var team = RequireTeam(teams, id);
        ValidateName(teams, name, id);
        if (password is not null) ValidatePassword(password);
        if (team.IsAdmin && !isAdmin && teams.Count(t => t.IsAdmin) == 1)
            throw new InvalidOperationException("The last administrator cannot be demoted.");
        if (password is not null) team.PasswordHash = _hasher.HashPassword(team, password);
        if (password is not null || team.IsAdmin != isAdmin)
            team.SessionVersion = Guid.NewGuid().ToString("N");
        team.Name = name.Trim();
        team.IsAdmin = isAdmin;
        await WriteAsync(team);
    });

    public Task DeleteAsync(string id) => LockedAsync(async () =>
    {
        var teams = await ReadAllAsync();
        var team = RequireTeam(teams, id);
        if (team.IsAdmin && teams.Count(t => t.IsAdmin) == 1)
            throw new InvalidOperationException("The last administrator cannot be deleted.");
        var registry = await ReadRegistryAsync();
        registry.Teams.RemoveAll(t => t.Id == id);
        await WriteFileAsync(RegistryPath, registry);
        File.Delete(DataPath(team.Name));
    });

    public Task SaveUrlAsync(string teamId, int? id, string url, string description,
        string environment, IEnumerable<string> projects) => ChangeAsync(teamId, team =>
    {
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(environment))
            throw new InvalidOperationException("A URL, description, and valid environment are required.");
        var selectedEnvironment = team.Environments!.SingleOrDefault(e => e.Equals(environment.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Select an environment belonging to this team.");
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var address) || (address.Scheme != "http" && address.Scheme != "https"))
            throw new InvalidOperationException("The URL must start with http:// or https://.");
        var names = projects.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (names.Count == 0) throw new InvalidOperationException("Select at least one project.");
        var selected = names.Select(name => team.Projects.SingleOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Project '{name}' was not found in this team.")).ToList();
        var entry = id.HasValue
            ? team.Urls.SingleOrDefault(u => u.Id == id.Value) ?? throw new InvalidOperationException("The requested URL was not found in this team.")
            : new TeamUrl { Id = team.NextUrlId++ };
        entry.UrlValue = url.Trim();
        entry.Description = description.Trim();
        entry.Environment = selectedEnvironment;
        entry.ProjectIds = selected.Select(p => p.Id).ToList();
        if (!id.HasValue) team.Urls.Add(entry);
    });

    public Task DeleteUrlAsync(string teamId, int id) => ChangeAsync(teamId, team =>
    {
        var entry = team.Urls.SingleOrDefault(u => u.Id == id)
            ?? throw new InvalidOperationException("The requested URL was not found in this team.");
        team.Urls.Remove(entry);
    });

    public Task SaveProjectAsync(string teamId, int? id, string name, string? color = null) => ChangeAsync(teamId, team =>
    {
        if (color is not null && !ProjectColor.IsValid(color))
            throw new InvalidOperationException("Choose a valid project color (#RRGGBB).");
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Project name is required.");
        if (team.Projects.Any(p => p.Id != id && p.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("A project with this name already exists in this team.");
        var project = id.HasValue
            ? team.Projects.SingleOrDefault(p => p.Id == id.Value) ?? throw new InvalidOperationException("Project not found in this team.")
            : new TeamProject { Id = team.NextProjectId++ };
        project.Name = name.Trim();
        project.Color = ProjectColor.Resolve(color ?? project.Color, project.Id);
        if (!id.HasValue) team.Projects.Add(project);
    });

    public Task DeleteProjectAsync(string teamId, int id) => ChangeAsync(teamId, team =>
    {
        var project = team.Projects.SingleOrDefault(p => p.Id == id)
            ?? throw new InvalidOperationException("Project not found in this team.");
        if (team.Urls.Any(u => u.ProjectIds.Contains(id)))
            throw new InvalidOperationException("Remove this project from its URLs before deleting it.");
        team.Projects.Remove(project);
    });

    public Task SaveEnvironmentAsync(string teamId, string name, string? originalName = null) => ChangeAsync(teamId, team =>
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100)
            throw new InvalidOperationException("Environment name must contain 1 to 100 characters.");
        var environments = team.Environments!;
        var index = originalName is null ? -1 : environments.FindIndex(e => e.Equals(originalName, StringComparison.OrdinalIgnoreCase));
        if (originalName is not null && index < 0) throw new InvalidOperationException("Environment not found in this team.");
        if (environments.Where((_, i) => i != index).Any(e => e.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("An environment with this name already exists in this team.");
        if (index < 0) environments.Add(name.Trim());
        else
        {
            var previous = environments[index];
            environments[index] = name.Trim();
            foreach (var url in team.Urls.Where(u => u.Environment.Equals(previous, StringComparison.OrdinalIgnoreCase)))
                url.Environment = name.Trim();
        }
    });

    public Task DeleteEnvironmentAsync(string teamId, string name) => ChangeAsync(teamId, team =>
    {
        var environment = team.Environments!.SingleOrDefault(e => e.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Environment not found in this team.");
        if (team.Urls.Any(u => u.Environment.Equals(environment, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Move or delete this environment's URLs before deleting it.");
        team.Environments!.Remove(environment);
    });

    private Task ChangeAsync(string id, Action<Team> change) => LockedAsync(async () =>
    {
        var team = RequireTeam(await ReadAllAsync(), id);
        change(team);
        await WriteAsync(team);
    });

    private async Task<Team> CreateCoreAsync(List<Team> teams, string name, string password, bool isAdmin)
    {
        ValidateName(teams, name);
        ValidatePassword(password);
        var team = new Team { Name = name.Trim(), IsAdmin = isAdmin, Environments = new() };
        team.PasswordHash = _hasher.HashPassword(team, password);
        await WriteAsync(team);
        return team;
    }

    private static void ValidateName(List<Team> teams, string name, string? id = null)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100)
            throw new InvalidOperationException("Team name must contain 1 to 100 characters.");
        ValidateFileName(name.Trim());
        if (teams.Any(t => t.Id != id && t.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("A team with this name already exists.");
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 4 || password.Length > 100)
            throw new InvalidOperationException("Team password must contain 4 to 100 characters.");
    }

    private static Team RequireTeam(List<Team> teams, string id) => teams.SingleOrDefault(t => t.Id == id)
        ?? throw new InvalidOperationException("Team not found.");

    private string RegistryPath => Path.Combine(_directory, "teams.yaml");

    private static void ValidateFileName(string name)
    {
        if (name != name.Trim() || !Regex.IsMatch(name, @"^[\p{L}\p{N}][\p{L}\p{N} _-]{0,99}$")
            || name.Equals("teams", StringComparison.OrdinalIgnoreCase)
            || Regex.IsMatch(name, @"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])$", RegexOptions.IgnoreCase))
            throw new InvalidOperationException("Team names can contain letters, numbers, spaces, underscores and hyphens. Choose another name if it is reserved.");
    }

    private string DataPath(string name)
    {
        ValidateFileName(name);
        return Path.Combine(_directory, name + ".yaml");
    }

    private async Task<TeamRegistry> ReadRegistryAsync()
    {
        if (!File.Exists(RegistryPath))
        {
            // Preserve files created by the previous per-team YAML layout.
            var legacyFiles = Directory.GetFiles(_directory, "*.yaml")
                .Where(p => Guid.TryParseExact(Path.GetFileNameWithoutExtension(p), "N", out _)).ToList();
            var migrated = new TeamRegistry();
            var legacyTeams = new List<Team>();
            foreach (var path in legacyFiles)
            {
                var team = await ReadFileAsync<Team>(path);
                ValidateName(legacyTeams, team.Name);
                if (team.Id != Path.GetFileNameWithoutExtension(path)) throw new InvalidDataException("Invalid legacy team identifier.");
                ValidateDocument(team);
                legacyTeams.Add(team);
                migrated.Teams.Add(MetadataFor(team));
            }
            foreach (var team in legacyTeams) await WriteFileAsync(DataPath(team.Name), DataFor(team));
            if (legacyTeams.Count > 0)
            {
                await WriteFileAsync(RegistryPath, migrated);
                foreach (var path in legacyFiles) File.Delete(path);
            }
            return migrated;
        }
        var registry = await ReadFileAsync<TeamRegistry>(RegistryPath);
        if (registry.Teams is null || registry.Teams.Any(t => t is null || !Guid.TryParseExact(t.Id, "N", out _)
                || string.IsNullOrWhiteSpace(t.Name) || string.IsNullOrWhiteSpace(t.PasswordHash) || string.IsNullOrWhiteSpace(t.SessionVersion))
            || registry.Teams.Select(t => t.Id).Distinct().Count() != registry.Teams.Count
            || registry.Teams.Select(t => t.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != registry.Teams.Count)
            throw new InvalidDataException("Invalid teams.yaml. The file has not been changed.");
        foreach (var team in registry.Teams) ValidateFileName(team.Name);
        return registry;
    }

    private async Task<List<Team>> ReadAllAsync()
    {
        var registry = await ReadRegistryAsync();
        var result = new List<Team>();
        foreach (var metadata in registry.Teams)
        {
            var data = await ReadFileAsync<TeamData>(DataPath(metadata.Name));
            if (data.TeamId != metadata.Id) throw new InvalidDataException($"The team identifier in '{metadata.Name}.yaml' does not match teams.yaml.");
            var team = new Team
            {
                Id = metadata.Id, Name = metadata.Name, PasswordHash = metadata.PasswordHash,
                IsAdmin = metadata.IsAdmin, SessionVersion = metadata.SessionVersion,
                NextProjectId = data.NextProjectId, NextUrlId = data.NextUrlId, Projects = data.Projects, Urls = data.Urls,
                Environments = data.Environments
            };
            ValidateDocument(team);
            result.Add(team);
        }
        return result.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task<T> ReadFileAsync<T>(string path) where T : class
    {
        try
        {
            var yaml = await File.ReadAllTextAsync(path, Encoding.UTF8);
            return _deserializer.Deserialize<T>(yaml) ?? throw new InvalidDataException("Empty YAML document.");
        }
        catch (Exception ex) when (ex is YamlException or InvalidDataException)
        {
            throw new InvalidDataException($"Cannot read '{Path.GetFileName(path)}'. The file has not been changed.", ex);
        }
    }

    private static void ValidateDocument(Team team)
    {
            // Older YAML files have no environment list. Preserve the names used by their URLs.
            team.Environments ??= team.Urls?.Where(u => u is not null).Select(u => u.Environment)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new();
            if (!Guid.TryParseExact(team.Id, "N", out _) || string.IsNullOrWhiteSpace(team.Name)
                || string.IsNullOrWhiteSpace(team.PasswordHash) || string.IsNullOrWhiteSpace(team.SessionVersion)
                || team.Projects is null || team.Urls is null
                || team.Projects.Any(p => p is null || p.Id <= 0 || string.IsNullOrWhiteSpace(p.Name)
                    || (!string.IsNullOrEmpty(p.Color) && !ProjectColor.IsValid(p.Color)))
                || team.Projects.Select(p => p.Id).Distinct().Count() != team.Projects.Count
                || team.Projects.Select(p => p.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != team.Projects.Count
                || team.Environments.Any(e => string.IsNullOrWhiteSpace(e) || e.Length > 100 || e != e.Trim())
                || team.Environments.Distinct(StringComparer.OrdinalIgnoreCase).Count() != team.Environments.Count
                || team.Urls.Any(u => u is null || u.Id <= 0 || u.ProjectIds is null || string.IsNullOrWhiteSpace(u.Environment)
                    || !team.Environments.Contains(u.Environment, StringComparer.OrdinalIgnoreCase)
                    || string.IsNullOrWhiteSpace(u.UrlValue) || string.IsNullOrWhiteSpace(u.Description)
                    || u.ProjectIds.Any(id => !team.Projects.Any(p => p.Id == id)))
                || team.Urls.Select(u => u.Id).Distinct().Count() != team.Urls.Count)
                throw new InvalidDataException("Invalid team document.");
            team.NextUrlId = Math.Max(team.NextUrlId, team.Urls.Select(u => u.Id).DefaultIfEmpty().Max() + 1);
            team.NextProjectId = Math.Max(team.NextProjectId, team.Projects.Select(p => p.Id).DefaultIfEmpty().Max() + 1);
            foreach (var project in team.Projects)
                project.Color = ProjectColor.Resolve(project.Color, project.Id);
            foreach (var url in team.Urls)
                url.Environment = team.Environments.Single(e => e.Equals(url.Environment, StringComparison.OrdinalIgnoreCase));
    }

    private async Task WriteAsync(Team team)
    {
        ValidateDocument(team);
        var registry = await ReadRegistryAsync();
        var previous = registry.Teams.SingleOrDefault(t => t.Id == team.Id);
        if (File.Exists(DataPath(team.Name)) && (previous is null || !previous.Name.Equals(team.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("A YAML file with this team name already exists. Choose another team name.");
        await WriteFileAsync(DataPath(team.Name), DataFor(team));
        if (previous is null || previous.Name != team.Name || previous.PasswordHash != team.PasswordHash
            || previous.IsAdmin != team.IsAdmin || previous.SessionVersion != team.SessionVersion)
        {
            registry.Teams.RemoveAll(t => t.Id == team.Id);
            registry.Teams.Add(MetadataFor(team));
            await WriteFileAsync(RegistryPath, registry);
            if (previous is not null && !previous.Name.Equals(team.Name, StringComparison.OrdinalIgnoreCase))
                File.Delete(DataPath(previous.Name));
        }
    }

    private static TeamMetadata MetadataFor(Team team) => new()
    {
        Id = team.Id, Name = team.Name, PasswordHash = team.PasswordHash,
        IsAdmin = team.IsAdmin, SessionVersion = team.SessionVersion
    };

    private static TeamData DataFor(Team team) => new()
    {
        TeamId = team.Id, NextUrlId = team.NextUrlId, NextProjectId = team.NextProjectId,
        Projects = team.Projects, Urls = team.Urls, Environments = team.Environments
    };

    private async Task WriteFileAsync<T>(string path, T data)
    {
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporary, _serializer.Serialize(data), new UTF8Encoding(false));
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private async Task<T> LockedAsync<T>(Func<Task<T>> action)
    {
        await _gate.WaitAsync();
        try
        {
            // One short-lived lock also serializes separate app processes using this folder.
            var deadline = DateTime.UtcNow.AddSeconds(10);
            FileStream fileLock;
            while (true)
            {
                try
                {
                    fileLock = new FileStream(Path.Combine(_directory, ".store.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    break;
                }
                catch (IOException) when (DateTime.UtcNow < deadline) { await Task.Delay(25); }
            }
            using (fileLock) return await action();
        }
        finally { _gate.Release(); }
    }

    private Task LockedAsync(Func<Task> action) => LockedAsync(async () => { await action(); return true; });
}
