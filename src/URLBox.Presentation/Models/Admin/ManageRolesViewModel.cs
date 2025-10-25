using System;
using System.Collections.Generic;

namespace URLBox.Presentation.Models.Admin
{
    public class ManageRolesViewModel
    {
        public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();

        public CreateRoleInputModel CreateRole { get; set; } = new();
    }
}
