using System;
using System.Collections.Generic;

namespace URLBox.Presentation.Models
{
    public class UserRoleViewModel
    {
        public string UserId { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();

        public int OwnedUrlCount { get; set; }
    }
}
