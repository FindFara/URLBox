using System.ComponentModel.DataAnnotations;

namespace URLBox.Presentation.Models
{
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Team name")]
        public string TeamName { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
}
