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

    public async Task UpdateAsync(Project project)
    {
        var entity = await _context.Projects.FirstOrDefaultAsync(p => p.Id == project.Id);
        if (entity is null)
        {
            throw new InvalidOperationException($"Project with ID {project.Id} was not found.");
        }

        entity.Name = project.Name;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int projectId)
    {
        var entity = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
        if (entity is null)
        {
            throw new InvalidOperationException($"Project with ID {projectId} was not found.");
        }

        _context.Projects.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludingId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var normalized = name.Trim().ToUpperInvariant();
        return await _context.Projects
            .AsNoTracking()
            .Where(p => !excludingId.HasValue || p.Id != excludingId.Value)
            .AnyAsync(p => p.Name.ToUpper() == normalized);
    }

    public async Task AssignRoleAsync(int projectId, string roleId)
    {
        var project = await _context.Projects
            .Include(p => p.Roles)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project is null)
        {
            throw new InvalidOperationException($"Project with ID {projectId} was not found.");
        }

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId);
        if (role is null)
        {
            throw new InvalidOperationException($"Role with ID {roleId} was not found.");
        }

        if (project.Roles.Any(r => string.Equals(r.Id, roleId, StringComparison.Ordinal)))
        {
            return;
        }

        project.Roles.Add(role);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveRoleAsync(int projectId, string roleId)
    {
        var project = await _context.Projects
            .Include(p => p.Roles)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project is null)
        {
            throw new InvalidOperationException($"Project with ID {projectId} was not found.");
        }

        var role = project.Roles.FirstOrDefault(r => string.Equals(r.Id, roleId, StringComparison.Ordinal));
        if (role is null)
        {
            return;
        }

        project.Roles.Remove(role);
        await _context.SaveChangesAsync();
    }
}
