using System.Collections.Generic;

namespace URLBox.Application.ViewModel
{
    public class UrlViewModel
    {
        public int Id { get; set; }
        public string TeamId { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;

        public string UrlValue { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public List<string> ProjectTags { get; set; } = new();

        public int Order { get; set; }

        public string Environment { get; set; } = string.Empty;

        public bool CanManage { get; set; }

    }
}
