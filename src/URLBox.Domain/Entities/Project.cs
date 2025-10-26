using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace URLBox.Domain.Entities
{
    public class Project
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public required string Name { get; set; }
        public ICollection<ApplicationRole> Roles { get; set; } = new List<ApplicationRole>();
        public ICollection<Url> Urls { get; set; } = new List<Url>();
    }
}
