namespace BuildingBlocks.Messaging.Events.UserLoginEvents;

public class UserLoginEvent
{
    public required Guid UserId { get; set; }
}