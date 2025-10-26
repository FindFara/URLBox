using System.Collections.Generic;
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
        public EnvironmentType Environment { get; set; }

        public bool IsPublic { get; set; }

        public string? CreatedByUserId { get; set; }

        public ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}
