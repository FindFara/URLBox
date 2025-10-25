using URLBox.Domain.Entities;

namespace URLBox.Domain.Interfaces;
public interface IProjectRepository
{
    Task<IEnumerable<Project>> GetAllAsync();
    Task AddAsync(Project project);
    Task<Project?> GetProject(string projectName);
}