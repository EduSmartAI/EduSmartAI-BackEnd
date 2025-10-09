using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.UserBehaviours.Commands;

public class UserBehaviourInsertCommand : ICommand<UserBehaviourInsertResponse>
{
    [Required(ErrorMessage = "ActionType is required")]
    public string ActionType { get; set; } = null!;
    
    public Guid? TargetId { get; set; }
    
    public string? TargetType { get; set; }
    
    public string? Metadata { get; set; }
}

