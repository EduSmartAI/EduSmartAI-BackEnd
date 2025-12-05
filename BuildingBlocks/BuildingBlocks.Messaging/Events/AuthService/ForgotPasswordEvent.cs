namespace BuildingBlocks.Messaging.Events.AuthService;

public class ForgotPasswordEvent
{
    public required string Email { get; set; } = null!;
    
    public required string Key { get; set; } = null!;
}