
using URLBox.Domain.Entities;

namespace URLBox.Domain.Interfaces
{
    public interface ITeamRepository
    {
        Task<IEnumerable<ApplicationRole>> GetAllAsync();
        Task AddAsync(ApplicationRole project);
    }
}
