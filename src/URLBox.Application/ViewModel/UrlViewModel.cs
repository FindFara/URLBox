using URLBox.Domain.Enums;

namespace URLBox.Application.ViewModel
{
    public class UrlViewModel
    {
        public int Id { get; set; }

        public string UrlValue { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string ProjectName { get; set; } = string.Empty;

        public EnvironmentType Environment { get; set; }
    }
}
