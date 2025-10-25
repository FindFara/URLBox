using URLBox.Domain.Entities;

namespace URLBox.Domain.Interfaces
{
    public interface IProjectRepository
    {
        Task<IEnumerable<Project>> GetAllAsync();

        Task<Project?> GetByNameAsync(string projectName);

        Task AddAsync(Project project);
    }
}
