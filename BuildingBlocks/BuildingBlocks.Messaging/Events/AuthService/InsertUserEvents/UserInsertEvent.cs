namespace BuildingBlocks.Messaging.Events.InsertUserEvents;

public record UserInsertEvent : IntegrationEvent
{
    public Guid UserId { get; set; }
    
    public Guid? OldUserId { get; set; }
    
    public string Email { get; set; } = null!;
    
    public string FirstName { get; set; } = null!;
    
    public string LastName { get; set; } = null!;
    
    public byte UserRole { get; set; }
}