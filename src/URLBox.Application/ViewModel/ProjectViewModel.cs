using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace URLBox.Application.ViewModel
{
    public class ProjectViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
