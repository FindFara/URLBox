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

        public int PublicUrlCount { get; set; }

        public int PrivateUrlCount { get; set; }

        public int UsersWithoutRoleCount { get; set; }

        public IReadOnlyList<ChartDataPoint> UsersPerRole { get; set; } = Array.Empty<ChartDataPoint>();

        public IReadOnlyList<ChartDataPoint> UrlsPerRole { get; set; } = Array.Empty<ChartDataPoint>();
    }
}
