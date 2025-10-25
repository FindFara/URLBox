using System.ComponentModel.DataAnnotations;

namespace URLBox.Presentation.Models.Admin
{
    public class CreateRoleInputModel
    {
        [Required]
        [Display(Name = "Role name")]
        public string RoleName { get; set; } = string.Empty;
    }
}
