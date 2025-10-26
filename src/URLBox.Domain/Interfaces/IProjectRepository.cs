using System.Collections.Generic;
using URLBox.Domain.Entities;

namespace URLBox.Domain.Interfaces;
public interface IProjectRepository
{
    Task<IEnumerable<Project>> GetAllAsync();
    Task<IEnumerable<Project>> GetProjectsForRolesAsync(IEnumerable<string> roleNames);
    Task<IDictionary<string, int>> GetProjectIdsByNamesAsync(IEnumerable<string> projectNames);
    Task AddAsync(Project project);
    Task UpdateAsync(Project project);
    Task DeleteAsync(int projectId);
    Task<bool> ExistsByNameAsync(string name, int? excludingId = null);
}