namespace AuthService.Application.Interfaces.TokenServices;

public class UserSessionResponse
{
    public Guid UserId { get; set; }
    
    public string Name { get; set; }
    
    public string Email { get; set; }
    public string Role { get; set; }
}