using System.Collections.Generic;

namespace URLBox.Application.ViewModel
{
    public class RoleUrlCountViewModel
    {
        public string RoleName { get; set; } = string.Empty;

        public int UrlCount { get; set; }
    }

    public class UrlStatisticsViewModel
    {
        public int TotalUrls { get; set; }

        public int PublicUrls { get; set; }

        public List<RoleUrlCountViewModel> UrlsPerRole { get; set; } = new();
    }
}
