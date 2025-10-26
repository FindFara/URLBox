using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using URLBox.Application.ViewModel;

namespace URLBox.Presentation.Models
{
    public class RoleSummaryViewModel
    {
        public string RoleId { get; set; } = string.Empty;

        public string RoleName { get; set; } = string.Empty;

        public int AssignedUserCount { get; set; }

        public List<ProjectViewModel> AssignedProjects { get; set; } = new();
    }

    public class CreateUserInputModel
    {
        [Required]
        [Display(Name = "User name")]
        [StringLength(256)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class UpdateUserInputModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(256)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;
    }

    public class UpdateRoleInputModel
    {
        [Required]
        public string RoleId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Role name")]
        [StringLength(256)]
        public string RoleName { get; set; } = string.Empty;
    }
}