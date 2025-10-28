using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.InsertUserEvents;

namespace StudentService.Application.Applications.Students.Commands.Inserts;

public record StudentInsertCommand : ICommand<UserInsertEventResponse>
{
    [Required(ErrorMessage = "UserId is required")]
    public Guid UserId { get; init; }
    
    public Guid? OldUserId { get; init; }
    
    [Required(ErrorMessage = "Email is required")]
    public string Enail { get; init; } = null!;
    
    [Required(ErrorMessage = "FirstName is required")]
    public string FirstName { get; init; } = null!;
    
    [Required(ErrorMessage = "LastName is required")]
    public string LastName { get; init; } = null!;
    
    [Required(ErrorMessage = "UserRole is required")]
    public byte UserRole { get; init; }
}