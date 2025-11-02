namespace BuildingBlocks.Messaging.Events.AuthService.InsertUserEvents;

public record LecturerInsertEvent : IntegrationEvent
{
    public Guid UserId { get; set; }
    
    public Guid? OldUserId { get; set; }
    
    public string Email { get; set; } = null!;
    
    public string FirstName { get; set; } = null!;
    
    public string LastName { get; set; } = null!;
}