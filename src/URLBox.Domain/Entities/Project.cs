using System.ComponentModel.DataAnnotations;

namespace URLBox.Domain.Entities
{
    public class Project
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;
    }
}