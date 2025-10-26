using System.Collections.Generic;
using URLBox.Application.ViewModel;

namespace URLBox.Presentation.Models
{
    public class AdminDashboardViewModel
    {
        public List<UserRoleViewModel> Users { get; set; } = new();

        public List<RoleSummaryViewModel> Roles { get; set; } = new();

        public string? StatusMessage { get; set; }

        public int TotalUsers { get; set; }

        public int TotalRoles { get; set; }

        public UrlStatisticsViewModel UrlStatistics { get; set; } = new();

        public List<ProjectViewModel> Projects { get; set; } = new();
    }
}
