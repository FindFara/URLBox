
using Microsoft.EntityFrameworkCore;
using System.Linq;
using URLBox.Domain.Entities;
using URLBox.Domain.Interfaces;
using URLBox.Infrastructure.Persistance;

namespace URLBox.Infrastructure.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly ApplicationDbContext _context;

    public ProjectRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Project>> GetAllAsync()
    {
        return await _context.Projects
            .AsNoTracking()
            .Select(p => new Project
            {
                Id = p.Id,
                Name = p.Name
            })
            .ToListAsync();
    }

    public async Task AddAsync(Project project)
    {
        var entity = new Project
        {
            Name = project.Name
        };
        _context.Projects.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<Project> GetProject(string projectName)
    {
        return await _context.Projects
                             .FirstOrDefaultAsync(p => p.Name == projectName);
    }
}