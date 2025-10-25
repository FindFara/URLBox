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
            bool publicOnly = false,
            bool includePublic = false,
            string? ownerId = null)
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
                if (allowedSet.Count == 0)
                {
                    filtered = includePublic
                        ? items.Where(url =>
                            url.IsPublic ||
                            (!string.IsNullOrEmpty(ownerId) && string.Equals(url.CreatedByUserId, ownerId, StringComparison.Ordinal)))
                        : Enumerable.Empty<Url>();
                }
                else
                {
                    filtered = items.Where(url =>
                        (url.Project is not null && allowedSet.Contains(url.Project.Name)) ||
                        (includePublic && url.IsPublic) ||
                        (!string.IsNullOrEmpty(ownerId) && string.Equals(url.CreatedByUserId, ownerId, StringComparison.Ordinal)));
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
                OwnerId = i.CreatedByUserId ?? string.Empty,
                OwnerName = i.CreatedByUser?.UserName ?? i.CreatedByUser?.Email ?? string.Empty,
            }).ToList();
        }

        public async Task AddUrlAsync(string urlValue, string description, EnvironmentType environment, string project, string ownerId, bool isPublic)
        {
            var projectEntity = await _projectRepository.GetProject(project)
                ?? throw new InvalidOperationException($"Project '{project}' was not found.");

            var url = new Url
            {
                UrlValue = urlValue,
                Description = description,
                Environment = environment,
                ProjectId = projectEntity.Id,
                CreatedByUserId = ownerId,
                IsPublic = isPublic
            };

            await _repository.AddAsync(url);
        }

        public async Task<bool> DeleteUrlAsync(int id, string? requesterId, bool isAdmin)
        {
            var url = await _repository.GetByIdAsync(id);
            if (url is null)
            {
                return false;
            }

            if (!isAdmin && !string.Equals(url.CreatedByUserId, requesterId, StringComparison.Ordinal))
            {
                return false;
            }

            await _repository.DeleteAsync(id);
            return true;
        }

        public async Task<bool> UpdateVisibilityAsync(int id, bool isPublic, string? requesterId, bool isAdmin)
        {
            var url = await _repository.GetByIdAsync(id);
            if (url is null)
            {
                return false;
            }

            if (!isAdmin && !string.Equals(url.CreatedByUserId, requesterId, StringComparison.Ordinal))
            {
                return false;
            }

            if (url.IsPublic == isPublic)
            {
                return true;
            }

            url.IsPublic = isPublic;
            await _repository.UpdateAsync(url);
            return true;
        }
    }
}
