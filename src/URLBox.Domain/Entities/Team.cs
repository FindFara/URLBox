using System.ComponentModel.DataAnnotations;

namespace URLBox.Domain.Entities
{
    public class Team
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string Name { get; set; }

        public ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}
