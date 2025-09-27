using Microsoft.EntityFrameworkCore;
using URLBox.Domain.Entities;
using URLBox.Domain.Interfaces;
using URLBox.Infrastructure.Persistance;

namespace URLBox.Infrastructure.Repositories;

public class TeamRepository : ITeamRepository
{
    private readonly ApplicationDbContext _context;

    public TeamRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Team>> GetAllAsync()
    {
        return await _context.Teams
            .AsNoTracking()
            .Select(p => new Team
            {
                Id = p.Id,
                Name = p.Name
            })
            .ToListAsync();
    }

    public async Task AddAsync(Team team)
    {
        var entity = new Team
        {
            Name = team.Name
        };
        _context.Teams.Add(entity);
        await _context.SaveChangesAsync();
    }
}