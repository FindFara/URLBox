using Microsoft.EntityFrameworkCore;
using URLBox.Domain.Interfaces;
using URLBox.Infrastructure.Persistance;
using URLBox.Domain.Entities;
using URLBox.Domain.Enums;

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
            .AsNoTracking()
            .Select(u => new Url
            {
                Id = u.Id,
                UrlValue = u.UrlValue,
                Description = u.Description,
                Tag = u.Tag,
                Order = u.Order,
                Environment = (Domain.Enums.EnvironmentType)u.Environment
            })
            .ToListAsync();
    }

    public async Task AddAsync(Url url)
    {
        var entity = new Url
        {
            UrlValue = url.UrlValue,
            Description = url.Description,
            Tag = url.Tag,
            Order = url.Order,
            Environment = (EnvironmentType)url.Environment
        };
        _context.Urls.Add(entity);
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