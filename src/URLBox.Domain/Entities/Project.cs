using System.ComponentModel.DataAnnotations;

namespace URLBox.Domain.Entities
{
    public class Project
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string Name { get; set; }

        public int? TeamId { get; set; }

        public Team? Team { get; set; }

        public ICollection<Url> Urls { get; set; } = new List<Url>();
    }
}
