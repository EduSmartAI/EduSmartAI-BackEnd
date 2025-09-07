namespace AuthService.Application.Dtos;

public class UserLoginDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string RoleName { get; set; }
}