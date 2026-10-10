using URLBox.Application.ViewModel;
using URLBox.Domain.Entities;

namespace URLBox.Application;

public static class TeamViewModels
{
    public static List<ProjectViewModel> ToProjects(this Team team) => team.Projects
        .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
        .Select(p => new ProjectViewModel { Id = p.Id, Name = p.Name, Color = ProjectColor.Resolve(p.Color, p.Id) }).ToList();

    public static List<UrlViewModel> ToUrls(this Team team) => team.Urls.Select(u => new UrlViewModel
    {
        Id = u.Id,
        TeamId = team.Id,
        TeamName = team.Name,
        UrlValue = u.UrlValue,
        Description = u.Description,
        Environment = u.Environment,
        ProjectTags = team.Projects.Where(p => u.ProjectIds.Contains(p.Id)).Select(p => p.Name)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList(),
        CanManage = true
    }).OrderBy(u => u.Environment).ThenBy(u => u.Description, StringComparer.OrdinalIgnoreCase).ToList();
}
