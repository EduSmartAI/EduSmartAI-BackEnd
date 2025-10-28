namespace BuildingBlocks.Messaging.Events.AuthService.UserLoginEvents;

public class TeacherLoginEvent
{
    public required Guid UserId { get; set; }
}