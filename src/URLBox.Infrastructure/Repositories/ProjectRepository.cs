using Microsoft.EntityFrameworkCore;
using URLBox.Domain.Entities;
using URLBox.Domain.Interfaces;
using URLBox.Infrastructure.Persistance;

namespace URLBox.Infrastructure.Repositories;

//public class TeamRepository : ITeamRepository
//{
//    private readonly ApplicationDbContext _context;

//    public TeamRepository(ApplicationDbContext context)
//    {
//        _context = context;
//    }

//    public async Task<IEnumerable<ApplicationRole>> GetAllAsync()
//    {
//        return await _context.Teams
//            .AsNoTracking()
//            .Select(p => new ApplicationRole
//            {
//                Id = p.Id,
//                Title = p.Title
//            })
//            .ToListAsync();
//    }

//    public async Task AddAsync(ApplicationRole team)
//    {
//        var entity = new ApplicationRole
//        {
//            Title = team.Title
//        };
//        _context.Teams.Add(entity);
//        await _context.SaveChangesAsync();
//    }
//}