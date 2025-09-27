
using URLBox.Domain.Entities;

namespace URLBox.Domain.Interfaces
{
    public interface ITeamRepository
    {
        Task<IEnumerable<Team>> GetAllAsync();
        Task AddAsync(Team team);
    }
}
