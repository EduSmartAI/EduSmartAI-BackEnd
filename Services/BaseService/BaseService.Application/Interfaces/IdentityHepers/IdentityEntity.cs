namespace BaseService.Application.Interfaces.IdentityHepers;

public class IdentityEntity
{
    public Guid UserId { get; set; }
    
    public string Email { get; set; }
    
    public string FullName { get; set; }
    
    public string RoleName { get; set; }
    
    public string? AvatarUrl { get; set; }
}