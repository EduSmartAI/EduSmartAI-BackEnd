namespace UtilityService.Domain.Models;

public partial class CloudinaryConfig
{
    public string CloudApiKey { get; set; } = null!;

    public string CloudApiSecret { get; set; } = null!;

    public string CloudApiName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string UpdatedBy { get; set; } = null!;
}
