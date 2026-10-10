using URLBox.Domain.Entities;

namespace URLBox.Infrastructure.Storage;

internal sealed class TeamRegistry
{
    public List<TeamMetadata> Teams { get; set; } = new();
}

internal sealed class TeamMetadata
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public string SessionVersion { get; set; } = string.Empty;
}

internal sealed class TeamData
{
    public string TeamId { get; set; } = string.Empty;
    public int NextUrlId { get; set; } = 1;
    public int NextProjectId { get; set; } = 1;
    public List<TeamProject> Projects { get; set; } = new();
    public List<string>? Environments { get; set; }
    public List<TeamUrl> Urls { get; set; } = new();
}
