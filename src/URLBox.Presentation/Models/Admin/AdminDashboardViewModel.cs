using System.Collections.Generic;

namespace URLBox.Presentation.Models.Admin
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }

        public int TotalRoles { get; set; }

        public IReadOnlyDictionary<string, int> RoleDistribution { get; set; } = new Dictionary<string, int>();
    }
}
