using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using URLBox.Application.ViewModel;
using URLBox.Domain.Authorization;
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
            bool isAdmin = false)
        {
            var items = (await _repository.GetAllAsync()).ToList();
            var allowedSet = allowedProjects is null
                ? null
                : new HashSet<string>(allowedProjects, StringComparer.OrdinalIgnoreCase);

            IEnumerable<Url> filtered = items;

            if (!isAdmin && allowedSet is not null)
            {
                if (allowedSet.Count > 0)
                {
                    filtered = items.Where(url =>
                        (url.Projects.Any(project =>
                            !string.IsNullOrWhiteSpace(project.Name)
                            && allowedSet.Contains(project.Name))) ||
                        (!string.IsNullOrEmpty(currentUserId) &&
                         string.Equals(url.CreatedByUserId, currentUserId, StringComparison.Ordinal)));
                }
                else
                {
                    filtered = items.Where(url =>
                        (!string.IsNullOrEmpty(currentUserId) &&
                         string.Equals(url.CreatedByUserId, currentUserId, StringComparison.Ordinal)));
                }
            }

            return filtered
                .Select(item =>
                {
                    var projectNames = item.Projects
                        .Select(project => project.Name)
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    return new UrlViewModel
                    {
                        Description = item.Description,
                        Environment = item.Environment,
                        Id = item.Id,
                        UrlValue = item.UrlValue,
                        ProjectTags = projectNames,
                        CanManage = isAdmin
                            || (allowedSet is not null
                                && projectNames.Any(name => allowedSet.Contains(name)))
                            || (!string.IsNullOrEmpty(currentUserId)
                                && string.Equals(item.CreatedByUserId, currentUserId, StringComparison.Ordinal))
                    };
                })
                .OrderBy(vm => vm.Environment)
                .ThenBy(vm => vm.Description)
                .ToList();
        }

        public async Task AddUrlAsync(
            string urlValue,
            string description,
            EnvironmentType environment,
            IEnumerable<string> projects,
            string? createdByUserId,
            IEnumerable<string>? allowedProjects,
            bool isAdmin)
        {
            var projectList = projects?
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            if (projectList.Count == 0)
            {
                throw new ArgumentException("At least one project must be selected.", nameof(projects));
            }

            if (!isAdmin)
            {
                var allowedSet = new HashSet<string>(allowedProjects ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
                if (!projectList.All(project => allowedSet.Contains(project)))
                {
                    throw new UnauthorizedAccessException("You are not allowed to add URLs for one or more selected projects.");
                }
            }

            var projectIds = await _projectRepository.GetProjectIdsByNamesAsync(projectList);
            if (projectIds.Count != projectList.Count)
            {
                var missing = projectList
                    .Where(name => !projectIds.ContainsKey(name))
                    .ToList();

                throw new InvalidOperationException($"Project(s) '{string.Join(", ", missing)}' were not found.");
            }

            var url = new Url
            {
                UrlValue = urlValue,
                Description = description,
                Environment = environment,
                CreatedByUserId = createdByUserId
            };

            await _repository.AddAsync(url, projectIds.Values);
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
                var projectNames = entity.Projects
                    .Select(project => project.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var hasProjectAccess = allowedSet.Count > 0
                    && projectNames.Any(name => allowedSet.Contains(name));

                var canManage = hasProjectAccess
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
                TotalUrls = items.Count
            };

            var roleAssignments = items
                .SelectMany(url => url.Projects
                    .SelectMany(project => project.Roles)
                    .Where(role => !string.IsNullOrWhiteSpace(role.Name) && !AppRoles.IsSystemRole(role.Name))
                    .Select(role => new { role.Name, UrlId = url.Id }))
                .ToList();

            var grouped = roleAssignments
                .GroupBy(assignment => assignment.Name, StringComparer.OrdinalIgnoreCase)
                .Select(group => new RoleUrlCountViewModel
                {
                    RoleName = group.Key,
                    UrlCount = group
                        .Select(assignment => assignment.UrlId)
                        .Distinct()
                        .Count()
                })
                .OrderByDescending(group => group.UrlCount)
                .ThenBy(group => group.RoleName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            stats.UrlsPerRole = grouped;

            return stats;
        }
    }
}
