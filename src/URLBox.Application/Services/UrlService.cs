using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public async Task<IEnumerable<UrlViewModel>> GetUrlsAsync(IEnumerable<string>? allowedProjects = null)
        {
            var items = await _repository.GetAllAsync();
            IEnumerable<Url> filtered = items;

            if (allowedProjects is not null)
            {
                var allowedSet = new HashSet<string>(allowedProjects, StringComparer.OrdinalIgnoreCase);
                if (allowedSet.Count > 0)
                {
                    filtered = items.Where(url =>
                        url.Project is not null &&
                        allowedSet.Contains(url.Project.Name));
                }
                else
                {
                    filtered = Enumerable.Empty<Url>();
                }
            }

            return filtered.Select(i => new UrlViewModel
            {
                Description = i.Description,
                Environment = i.Environment,
                Id = i.Id,
                UrlValue = i.UrlValue,
                Tag = i.Project?.Name ?? string.Empty,
            }).ToList();
        }

        public async Task AddUrlAsync(string urlValue, string description, EnvironmentType environment, string project)
        {
            var projectEntity = await _projectRepository.GetProject(project)
                ?? throw new InvalidOperationException($"Project '{project}' was not found.");

            var url = new Url
            {
                UrlValue = urlValue,
                Description = description,
                Environment = environment,
                ProjectId = projectEntity.Id
            };

            await _repository.AddAsync(url);
        }

        public Task DeleteUrlAsync(int id) => _repository.DeleteAsync(id);
    }
}
