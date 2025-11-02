namespace AuthService.Domain.WriteModels;

public partial class AdminAccount
{
    public Guid AccountId { get; set; }
    
    public string FullName { get; set; } = null!;
    
    public string? PhoneNumber { get; set; }
    
    public short? Position { get; set; }
    
    public string? Note { get; set; }
    
    public bool IsSuperAdmin { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public string CreatedBy { get; set; } = null!;
    
    public DateTime UpdatedAt { get; set; }
    
    public string UpdatedBy { get; set; } = null!;
    
    public bool IsActive { get; set; }
    
    public virtual Account Account { get; set; } = null!;
}