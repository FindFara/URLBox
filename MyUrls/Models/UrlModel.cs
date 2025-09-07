namespace URLBox.Models
{
	public class UrlModel
	{
		public int Id { get; set; }
		public string? Url { get; set; } =string.Empty;
		public string? Description { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public int Order { get; set; }
        public EnvironmentType Environment { get; set; } 
    }
}
