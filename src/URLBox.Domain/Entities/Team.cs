namespace URLBox.Domain.Entities;

public sealed class Team
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public string SessionVersion { get; set; } = Guid.NewGuid().ToString("N");
    public int NextUrlId { get; set; } = 1;
    public int NextProjectId { get; set; } = 1;
    public List<TeamProject> Projects { get; set; } = new();
    public List<string>? Environments { get; set; }
    public List<TeamUrl> Urls { get; set; } = new();
}

public sealed class TeamProject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

public sealed class TeamUrl
{
    public int Id { get; set; }
    public string UrlValue { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public List<int> ProjectIds { get; set; } = new();
}
