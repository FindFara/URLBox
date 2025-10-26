using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace URLBox.Domain.Entities;

public class ApplicationRole : IdentityRole
{
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
