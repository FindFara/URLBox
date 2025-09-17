using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace URLBox.Domain.Entities
{
    public class Team
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; }
        public List<Project> Projects { get; set; }
    }
}
