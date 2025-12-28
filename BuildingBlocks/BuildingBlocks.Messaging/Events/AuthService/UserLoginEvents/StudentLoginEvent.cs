namespace BuildingBlocks.Messaging.Events.UserLoginEvents;

public class StudentLoginEvent
{
    public required Guid UserId { get; set; }
}