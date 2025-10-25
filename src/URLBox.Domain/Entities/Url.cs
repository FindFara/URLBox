using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using URLBox.Domain.Enums;

namespace URLBox.Domain.Entities
{
    public class Url
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey(nameof(ProjectId))]
        public Project Project { get; set; } = null!;

        [Required]
        [Url]
        public required string UrlValue { get; set; }

        [Required]
        public required string Description { get; set; }

        [Required]
        public EnvironmentType Environment { get; set; }
    }
}
