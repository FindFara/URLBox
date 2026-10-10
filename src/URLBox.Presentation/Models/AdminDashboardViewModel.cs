using URLBox.Application.ViewModel;
using URLBox.Domain.Entities;

namespace URLBox.Presentation.Models;

public class AdminDashboardViewModel
{
    public List<Team> Teams { get; set; } = new();
    public Team? SelectedTeam { get; set; }
    public List<ProjectViewModel> Projects { get; set; } = new();
    public List<UrlViewModel> Urls { get; set; } = new();
    public string? StatusMessage { get; set; }
    public string UrlSort { get; set; } = "description";
    public bool Descending { get; set; }
    public int TotalUrls { get; set; }
}
