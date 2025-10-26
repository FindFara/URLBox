using System;
using System.Linq;
using URLBox.Application.ViewModel;
using URLBox.Domain.Entities;
using URLBox.Domain.Interfaces;

namespace URLBox.Application.Services
{
    public class ProjectService
    {
        private readonly IProjectRepository _repository;

        public ProjectService(IProjectRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ProjectViewModel>> GetProjectsAsync()
        {
            var result = await _repository.GetAllAsync();
            return result
                .Select(x => new ProjectViewModel
                {
                    Id = x.Id,
                    Name = x.Name,
                })
                .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<IEnumerable<ProjectViewModel>> GetProjectsForRolesAsync(IEnumerable<string> roleNames)
        {
            var result = await _repository.GetProjectsForRolesAsync(roleNames);
            return result
                .Select(x => new ProjectViewModel
                {
                    Id = x.Id,
                    Name = x.Name,
                })
                .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task AddProjectAsync(string name)
        {
            var project = new Project { Name = name };
            await _repository.AddAsync(project);
        }
    }
}