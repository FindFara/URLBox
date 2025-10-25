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
            string? currentUserId = null,
            bool includeOnlyPublic = false,
            bool isAdmin = false)
        {
            var items = (await _repository.GetAllAsync()).ToList();
            var allowedSet = allowedProjects is null
                ? null
                : new HashSet<string>(allowedProjects, StringComparer.OrdinalIgnoreCase);

            IEnumerable<Url> filtered = items;

            if (includeOnlyPublic)
            {
                filtered = items.Where(url => url.IsPublic);
            }
            else if (!isAdmin && allowedSet is not null)
            {
                if (allowedSet.Count > 0)
                {
                    filtered = items.Where(url =>
                        url.IsPublic ||
                        (url.Project is not null && allowedSet.Contains(url.Project.Name)) ||
                        (!string.IsNullOrEmpty(currentUserId) &&
                         string.Equals(url.CreatedByUserId, currentUserId, StringComparison.Ordinal)));
                }
                else
                {
                    filtered = items.Where(url =>
                        url.IsPublic ||
                        (!string.IsNullOrEmpty(currentUserId) &&
                         string.Equals(url.CreatedByUserId, currentUserId, StringComparison.Ordinal)));
                }
            }

            return filtered
                .Select(i => new UrlViewModel
                {
                    Description = i.Description,
                    Environment = i.Environment,
                    Id = i.Id,
                    UrlValue = i.UrlValue,
                    Tag = i.Project?.Name ?? string.Empty,
                    IsPublic = i.IsPublic,
                    CanManage = isAdmin
                        || (allowedSet is not null && i.Project is not null && allowedSet.Contains(i.Project.Name))
                        || (!string.IsNullOrEmpty(currentUserId)
                            && string.Equals(i.CreatedByUserId, currentUserId, StringComparison.Ordinal))
                })
                .OrderBy(vm => vm.Environment)
                .ThenBy(vm => vm.Description)
                .ToList();
        }

        public async Task AddUrlAsync(
            string urlValue,
            string description,
            EnvironmentType environment,
            string project,
            bool isPublic,
            string? createdByUserId,
            IEnumerable<string>? allowedProjects,
            bool isAdmin)
        {
            if (!isAdmin)
            {
                var allowedSet = new HashSet<string>(allowedProjects ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
                if (!allowedSet.Contains(project))
                {
                    throw new UnauthorizedAccessException("You are not allowed to add URLs for this project.");
                }
            }

            var projectEntity = await _projectRepository.GetProject(project)
                ?? throw new InvalidOperationException($"Project '{project}' was not found.");

            var url = new Url
            {
                UrlValue = urlValue,
                Description = description,
                Environment = environment,
                ProjectId = projectEntity.Id,
                IsPublic = isPublic,
                CreatedByUserId = createdByUserId
            };

            await _repository.AddAsync(url);
        }

        public async Task DeleteUrlAsync(
            int id,
            IEnumerable<string>? allowedProjects,
            string? currentUserId,
            bool isAdmin)
        {
            var entity = await _repository.GetByIdAsync(id)
                ?? throw new InvalidOperationException("The requested URL could not be found.");

            if (!isAdmin)
            {
                var allowedSet = new HashSet<string>(allowedProjects ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
                var projectName = entity.Project?.Name;
                var canManage = (!string.IsNullOrEmpty(projectName) && allowedSet.Contains(projectName))
                    || (!string.IsNullOrEmpty(currentUserId)
                        && string.Equals(entity.CreatedByUserId, currentUserId, StringComparison.Ordinal));

                if (!canManage)
                {
                    throw new UnauthorizedAccessException("You are not allowed to delete this URL.");
                }
            }

            await _repository.DeleteAsync(id);
        }

        public async Task<UrlStatisticsViewModel> GetStatisticsAsync()
        {
            var items = (await _repository.GetAllAsync()).ToList();
            var stats = new UrlStatisticsViewModel
            {
                TotalUrls = items.Count,
                PublicUrls = items.Count(url => url.IsPublic)
            };

            var grouped = items
                .Where(url => url.Project is not null && !string.IsNullOrWhiteSpace(url.Project.Name))
                .GroupBy(url => url.Project!.Name!, StringComparer.OrdinalIgnoreCase)
                .Select(group => new RoleUrlCountViewModel
                {
                    RoleName = group.Key,
                    UrlCount = group.Count()
                })
                .OrderByDescending(group => group.UrlCount)
                .ThenBy(group => group.RoleName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            stats.UrlsPerRole = grouped;

            return stats;
        }
    }
}
