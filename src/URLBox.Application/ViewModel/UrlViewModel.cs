using URLBox.Domain.Enums;

namespace URLBox.Application.ViewModel
{
    public class UrlViewModel
    {
        public int Id { get; set; }

        public string UrlValue { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Tag { get; set; } = string.Empty;

        public int Order { get; set; }

        public EnvironmentType Environment { get; set; }

        public bool IsPublic { get; set; }

        public bool CanManage { get; set; }

    }
}
