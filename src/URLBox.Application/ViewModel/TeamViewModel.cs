using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using URLBox.Domain.Entities;

namespace URLBox.Application.ViewModel
{
    public class TeamViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public List<ProjectViewModel>? Projects { get; set; } = null;
    }
}
