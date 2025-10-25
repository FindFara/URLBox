using System;
using System.Collections.Generic;

namespace URLBox.Presentation.Models.Admin
{
    public class UserListItemViewModel
    {
        public required string Id { get; set; }

        public required string UserName { get; set; }

        public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
    }
}
