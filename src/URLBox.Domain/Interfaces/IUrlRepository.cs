using System.Collections.Generic;
using URLBox.Domain.Entities;

namespace URLBox.Domain.Interfaces;
public interface IUrlRepository
{
    Task<IEnumerable<Url>> GetAllAsync();
    Task AddAsync(Url url, IEnumerable<int> projectIds);
    Task DeleteAsync(int id);
    Task<Url?> GetByIdAsync(int id);
}