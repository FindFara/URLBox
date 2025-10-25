using System.Linq;
using Microsoft.EntityFrameworkCore;
using URLBox.Domain.Entities;
using URLBox.Domain.Interfaces;
using URLBox.Infrastructure.Persistance;

namespace URLBox.Infrastructure.Repositories
{
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
                .AsNoTracking()
                .Include(u => u.Project)
                .ToListAsync();
        }

        public async Task<IEnumerable<Url>> GetByProjectNamesAsync(IEnumerable<string> projectNames)
        {
            var normalized = projectNames
                .Select(name => name.Trim())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.ToLowerInvariant())
                .ToList();

            if (normalized.Count == 0)
            {
                return Array.Empty<Url>();
            }

            return await _context.Urls
                .AsNoTracking()
                .Include(u => u.Project)
                .Where(u => normalized.Contains(u.Project.Name.ToLower()))
                .ToListAsync();
        }

        public async Task AddAsync(Url url)
        {
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
    }
}
