using URLBox.Application.ViewModel;
using URLBox.Domain.Entities;
using URLBox.Domain.Interfaces;

//namespace URLBox.Application.Services
//{
//    public class TeamService
//    {
//        private readonly ITeamRepository _repository;

//        public TeamService(ITeamRepository repository)
//        {
//            _repository = repository;
//        }

//        public async Task<IEnumerable<TeamViewModel>> GetTeamsAsync()
//        {
//            var result = await _repository.GetAllAsync();
//            return result.Select(x => new TeamViewModel
//            {
//                Id = x.Id,
//                Title = x.Title,
//                Projects = x.Projects.Select(p => new ProjectViewModel
//                {
//                    Name = p.Name,
//                    Id = p.Id,
//                }).ToList(),
//            });
//        }
//        public async Task<IEnumerable<TeamViewModel>> GetTeamsOnly()
//        {
//            var result = await _repository.GetAllAsync();
//            return result.Select(x => new TeamViewModel
//            {
//                Id = x.Id,
//                Title = x.Title
//            });
//        }
//        public async Task AddTeamAsync(string name)
//        {
//            var project = new ApplicationRole { Title = name };
//            await _repository.AddAsync(project);
//        }
//    }
//}