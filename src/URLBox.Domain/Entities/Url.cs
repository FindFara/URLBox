using System.ComponentModel.DataAnnotations;
using URLBox.Domain.Enums;

namespace URLBox.Domain.Entities
{
    public class Url
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UrlValue { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Tag { get; set; } = string.Empty;

        public int Order { get; set; }

        [Required]
        public EnvironmentType Environment { get; set; }
    }
}