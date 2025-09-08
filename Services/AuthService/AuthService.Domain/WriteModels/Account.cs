namespace AuthService.Domain.WriteModels;

public partial class Account
{
    public Guid AccountId { get; set; }

    public Guid RoleId { get; set; }

    public string Email { get; set; } = null!;

    public bool EmailConfirmed { get; set; }

    public string? PasswordHash { get; set; }
    
    public DateTimeOffset? LockoutEnd { get; set; }

    public DateTime CreatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string UpdatedBy { get; set; } = null!;

    public int AccessFailedCount { get; set; }

    public string? Key { get; set; }
    
    public virtual Role Role { get; set; } = null!;
}
