using URLBox.Application.ViewModel;
using URLBox.Domain.Entities;
using URLBox.Domain.Enums;
using URLBox.Domain.Interfaces;

namespace URLBox.Application.Services
{
    public class UrlService
    {
        private readonly IUrlRepository _repository;
        private readonly IProjectRepository _projectRepository;


        public UrlService(IUrlRepository repository, IProjectRepository projectRepository)
        {
            _repository = repository;
            _projectRepository = projectRepository;
        }

        public async Task<IEnumerable<UrlViewModel>> GetUrlsAsync()
        {
            var items = await _repository.GetAllAsync();

            return items.Select(i => new UrlViewModel
            {
                Description = i.Description,
                Environment = i.Environment,
                Id = i.Id,
                UrlValue = i.UrlValue,
            }).ToList();
        }

        public async Task AddUrlAsync(string urlValue, string description, EnvironmentType environment, string project)
        {
            var projectdb = await _projectRepository.GetProject(project);

            var url = new Url
            {
                UrlValue = urlValue,
                Description = description,
                Environment = environment,
                ProjectId= projectdb.Id
            };
            await _repository.AddAsync(url);
        }
        public Task DeleteUrlAsync(int id) => _repository.DeleteAsync(id);
    }
}