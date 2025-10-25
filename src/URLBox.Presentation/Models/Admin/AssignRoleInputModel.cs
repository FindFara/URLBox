using System.ComponentModel.DataAnnotations;

namespace URLBox.Presentation.Models.Admin
{
    public class AssignRoleInputModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Role name")]
        public string RoleName { get; set; } = string.Empty;
    }
}
