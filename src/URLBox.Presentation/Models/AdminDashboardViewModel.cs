using System;
using System.Collections.Generic;

namespace URLBox.Presentation.Models
{
    public class AdminDashboardViewModel
    {
        public List<UserRoleViewModel> Users { get; set; } = new();

        public List<RoleSummaryViewModel> Roles { get; set; } = new();

        public string? StatusMessage { get; set; }

        public int TotalUsers { get; set; }

        public int TotalRoles { get; set; }

        public int TotalUrls { get; set; }

        public int TotalPublicUrls { get; set; }

        public int TotalPrivateUrls => Math.Max(0, TotalUrls - TotalPublicUrls);

        public List<string> RoleChartLabels { get; set; } = new();

        public List<int> RoleChartValues { get; set; } = new();
    }
}
