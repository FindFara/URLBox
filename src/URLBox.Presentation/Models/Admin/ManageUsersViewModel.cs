using System;
using System.Collections.Generic;

namespace URLBox.Presentation.Models.Admin
{
    public class ManageUsersViewModel
    {
        public IReadOnlyCollection<UserListItemViewModel> Users { get; set; } = Array.Empty<UserListItemViewModel>();

        public IReadOnlyCollection<string> AvailableRoles { get; set; } = Array.Empty<string>();

        public AssignRoleInputModel AssignRole { get; set; } = new();
    }
}
