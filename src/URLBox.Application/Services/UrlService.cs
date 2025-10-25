using System.Linq;
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
            return MapUrls(items);
        }

        public async Task<IEnumerable<UrlViewModel>> GetUrlsForProjectsAsync(IEnumerable<string> projectNames)
        {
            var items = await _repository.GetByProjectNamesAsync(projectNames);
            return MapUrls(items);
        }

        public async Task AddUrlAsync(string urlValue, string description, EnvironmentType environment, string projectName)
        {
            var project = await _projectRepository.GetByNameAsync(projectName)
                          ?? throw new InvalidOperationException($"Project '{projectName}' was not found.");

            var url = new Url
            {
                UrlValue = urlValue,
                Description = description,
                Environment = environment,
                ProjectId = project.Id
            };

            await _repository.AddAsync(url);
        }

        public Task DeleteUrlAsync(int id) => _repository.DeleteAsync(id);

        private static IEnumerable<UrlViewModel> MapUrls(IEnumerable<Url> items)
        {
            return items.Select(i => new UrlViewModel
            {
                Id = i.Id,
                UrlValue = i.UrlValue,
                Description = i.Description,
                Environment = i.Environment,
                ProjectName = i.Project.Name
            });
        }
    }
}
