using URLBox.Domain.Entities;

namespace URLBox.Domain.Interfaces
{
    public interface IUrlRepository
    {
        Task<IEnumerable<Url>> GetAllAsync();

        Task<IEnumerable<Url>> GetByProjectNamesAsync(IEnumerable<string> projectNames);

        Task AddAsync(Url url);

        Task DeleteAsync(int id);
    }
}
