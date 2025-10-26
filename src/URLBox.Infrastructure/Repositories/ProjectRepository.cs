using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
            .OrderBy(p => p.Name)
            .Select(p => new Project
            {
                Id = p.Id,
                Name = p.Name
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<Project>> GetProjectsForRolesAsync(IEnumerable<string> roleNames)
    {
        var roleList = roleNames?
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (roleList is null || roleList.Count == 0)
        {
            return Array.Empty<Project>();
        }

        return await _context.Projects
            .AsNoTracking()
            .Where(project => project.Roles.Any(role => roleList.Contains(role.Name)))
            .Select(project => new Project
            {
                Id = project.Id,
                Name = project.Name
            })
            .ToListAsync();
    }

    public async Task<IDictionary<string, int>> GetProjectIdsByNamesAsync(IEnumerable<string> projectNames)
    {
        var names = projectNames?
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (names is null || names.Count == 0)
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        var results = await _context.Projects
            .AsNoTracking()
            .Where(project => names.Contains(project.Name))
            .Select(project => new { project.Id, project.Name })
            .ToListAsync();

        return results.ToDictionary(
            item => item.Name,
            item => item.Id,
            StringComparer.OrdinalIgnoreCase);
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
}
