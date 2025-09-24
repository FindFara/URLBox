using Microsoft.AspNetCore.Identity;

namespace URLBox.Domain.Entities;
public class ApplicationRole : IdentityRole
{
    public List<Project> Projects { get; set; }
}
