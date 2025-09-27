using System.ComponentModel.DataAnnotations;

namespace URLBox.Domain.Entities
{
    public class Project
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public required string Name { get; set; }
        public Team Teams { get; set; }
        public List<Url> Urls { get; set; }
    }
}