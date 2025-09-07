using AuthService.Domain.Snapshort;
using AuthService.Domain.WriteModels;

namespace AuthService.Domain.ReadModels;

public class AccountCollection
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
    
    public UserInformation UserInformation { get; set; } = null!;
    
    public RoleCollection Role { get; set; } = null!;
    
    public static AccountCollection FromWriteModel(Account account, UserInformation userInformation, bool includeRelated = false)
    {
        var result = new AccountCollection
        {
            AccountId = account.AccountId,
            RoleId = account.RoleId,
            Email = account.Email,
            EmailConfirmed = account.EmailConfirmed,
            PasswordHash = account.PasswordHash,
            LockoutEnd = account.LockoutEnd,
            CreatedAt = account.CreatedAt,
            CreatedBy = account.CreatedBy,
            IsActive = account.IsActive,
            UpdatedAt = account.UpdatedAt,
            UpdatedBy = account.UpdatedBy,
            AccessFailedCount = account.AccessFailedCount,
            Key = account.Key,
            UserInformation = userInformation
        };
        
        if (includeRelated)
        {
            result.Role = RoleCollection.FromWriteModel(account.Role);
        }
        return result;
    }
}