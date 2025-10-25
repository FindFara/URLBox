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

        public async Task<IEnumerable<UrlViewModel>> GetUrlsAsync(
            IEnumerable<string>? allowedProjects = null,
            bool includePublic = false,
            bool publicOnly = false)
        {
            var items = await _repository.GetAllAsync();
            IEnumerable<Url> filtered = items;

            if (publicOnly)
            {
                filtered = items.Where(url => url.IsPublic);
            }
            else if (allowedProjects is not null)
            {
                var allowedSet = new HashSet<string>(allowedProjects, StringComparer.OrdinalIgnoreCase);
                if (allowedSet.Count > 0)
                {
                    filtered = items.Where(url =>
                        (includePublic && url.IsPublic) ||
                        (url.Project is not null &&
                         allowedSet.Contains(url.Project.Name)));
                }
                else
                {
                    filtered = includePublic
                        ? items.Where(url => url.IsPublic)
                        : Enumerable.Empty<Url>();
                }
            }

            return filtered.Select(i => new UrlViewModel
            {
                Description = i.Description,
                Environment = i.Environment,
                Id = i.Id,
                UrlValue = i.UrlValue,
                Tag = i.Project?.Name ?? string.Empty,
                IsPublic = i.IsPublic,
            }).ToList();
        }

        public async Task AddUrlAsync(string urlValue, string description, EnvironmentType environment, string project, bool isPublic)
        {
            var projectEntity = await _projectRepository.GetProject(project)
                ?? throw new InvalidOperationException($"Project '{project}' was not found.");

            var url = new Url
            {
                UrlValue = (urlValue ?? string.Empty).Trim(),
                Description = (description ?? string.Empty).Trim(),
                Environment = environment,
                ProjectId = projectEntity.Id,
                IsPublic = isPublic
            };

            await _repository.AddAsync(url);
        }

        public Task DeleteUrlAsync(int id) => _repository.DeleteAsync(id);

        public Task<Url?> GetUrlAsync(int id) => _repository.GetByIdAsync(id);

        public async Task UpdateVisibilityAsync(int id, bool isPublic)
        {
            var url = await _repository.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"URL with id '{id}' was not found.");

            if (url.IsPublic != isPublic)
            {
                url.IsPublic = isPublic;
                await _repository.UpdateAsync(url);
            }
        }
    }
}
