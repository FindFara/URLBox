using System.ComponentModel.DataAnnotations;

namespace URLBox.Presentation.Models;

public class TeamInputModel
{
    public string? Id { get; set; }
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;
    [StringLength(100, MinimumLength = 4), DataType(DataType.Password)]
    public string? Password { get; set; }
    public bool IsAdmin { get; set; }
}
