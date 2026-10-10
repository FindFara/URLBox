using URLBox.Domain.Entities;

namespace URLBox.Domain.Interfaces;

public interface ITeamStore
{
    Task<List<Team>> GetTeamsAsync();
    Task<Team?> GetAsync(string id);
    Task<Team?> AuthenticateAsync(string name, string password);
    Task EnsureAdminAsync(string name, string password, bool seedSampleData = false);
    Task<Team> CreateAsync(string name, string password, bool isAdmin = false);
    Task UpdateAsync(string id, string name, string? password, bool isAdmin);
    Task DeleteAsync(string id);
    Task SaveUrlAsync(string teamId, int? id, string url, string description, string environment, IEnumerable<string> projects);
    Task SaveEnvironmentAsync(string teamId, string name, string? originalName = null);
    Task DeleteEnvironmentAsync(string teamId, string name);
    Task DeleteUrlAsync(string teamId, int id);
    Task SaveProjectAsync(string teamId, int? id, string name, string? color = null);
    Task DeleteProjectAsync(string teamId, int id);
}
