using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.AuthService.InsertUserEvents;

namespace TeacherService.Application.Applications.Teachers.Commands.Inserts;

public record LecturerInsertCommand : ICommand<LecturerInsertEventResponse>
{
    [Required(ErrorMessage = "UserId is required")]
    public Guid UserId { get; init; }
    
    public Guid? OldUserId { get; init; }
    
    [Required(ErrorMessage = "Email is required")]
    public string Email { get; init; } = null!;
    
    [Required(ErrorMessage = "FirstName is required")]
    public string FirstName { get; init; } = null!;
    
    [Required(ErrorMessage = "LastName is required")]
    public string LastName { get; init; } = null!;
}

