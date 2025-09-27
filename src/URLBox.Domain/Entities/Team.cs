using System.ComponentModel.DataAnnotations;

namespace URLBox.Domain.Entities
{
    public class Team
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Project> Projects { get; set; }

    }
}
