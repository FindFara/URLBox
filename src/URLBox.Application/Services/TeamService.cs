using System.Linq;
using URLBox.Application.ViewModel;
using URLBox.Domain.Entities;
using URLBox.Domain.Interfaces;

namespace URLBox.Application.Services
{
    public class TeamService
    {
        private readonly ITeamRepository _repository;

        public TeamService(ITeamRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<TeamViewModel>> GetTeamsAsync()
        {
            var result = await _repository.GetAllAsync();
            return result.Select(x => new TeamViewModel
            {
                Id = x.Id,
                Name = x.Name
            });
        }

        public async Task<IEnumerable<TeamViewModel>> GetTeamsOnlyAsync()
        {
            return await GetTeamsAsync();
        }

        public async Task AddTeamAsync(string name)
        {
            var team = new Team { Name = name };
            await _repository.AddAsync(team);
        }
    }
}
