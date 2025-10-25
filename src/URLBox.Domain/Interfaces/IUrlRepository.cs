using URLBox.Domain.Entities;

namespace URLBox.Domain.Interfaces;
public interface IUrlRepository
{
    Task<IEnumerable<Url>> GetAllAsync();
    Task<Url?> GetByIdAsync(int id);
    Task AddAsync(Url url);
    Task DeleteAsync(int id);
    Task UpdateAsync(Url url);
}