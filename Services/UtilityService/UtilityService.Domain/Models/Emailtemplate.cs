namespace UtilityService.Domain.Models;

public partial class Emailtemplate
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public string CreateBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string UpdateBy { get; set; } = null!;

    public string Body { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string ScreenName { get; set; } = null!;
}
