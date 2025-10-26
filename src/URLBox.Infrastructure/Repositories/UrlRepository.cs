using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using URLBox.Domain.Entities;
using URLBox.Domain.Interfaces;
using URLBox.Infrastructure.Persistance;

namespace URLBox.Infrastructure.Repositories;

public class UrlRepository : IUrlRepository
{
    private readonly ApplicationDbContext _context;

    public UrlRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Url>> GetAllAsync()
    {
        return await _context.Urls
            .Include(u => u.Projects)
                .ThenInclude(p => p.Roles)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task AddAsync(Url url, IEnumerable<int> projectIds)
    {
        var projectIdList = projectIds?
            .Distinct()
            .ToList() ?? new List<int>();

        if (projectIdList.Count > 0)
        {
            var projects = await _context.Projects
                .Where(project => projectIdList.Contains(project.Id))
                .ToListAsync();

            foreach (var project in projects)
            {
                url.Projects.Add(project);
            }
        }

        _context.Urls.Add(url);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Urls.FindAsync(id);
        if (entity != null)
        {
            _context.Urls.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Url?> GetByIdAsync(int id)
    {
        return await _context.Urls
            .Include(u => u.Projects)
                .ThenInclude(p => p.Roles)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);
    }
}
