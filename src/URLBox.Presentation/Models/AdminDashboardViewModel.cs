using System.Collections.Generic;

namespace URLBox.Presentation.Models
{
    public class AdminDashboardViewModel
    {
        public List<UserRoleViewModel> Users { get; set; } = new();

        public List<string> Roles { get; set; } = new();

        public string? StatusMessage { get; set; }
    }
}
