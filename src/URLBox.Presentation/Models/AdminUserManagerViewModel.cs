using System.Collections.Generic;
using URLBox.Application.ViewModel;

namespace URLBox.Presentation.Models
{
    public class AdminUserManagerViewModel
    {
        public List<UserRoleViewModel> Users { get; set; } = new();

        public List<RoleSummaryViewModel> Roles { get; set; } = new();

        public string? StatusMessage { get; set; }

        public int UserPage { get; set; }

        public int UserPageSize { get; set; }

        public int UserTotalCount { get; set; }

        public int UserTotalPages { get; set; }
    }
}
