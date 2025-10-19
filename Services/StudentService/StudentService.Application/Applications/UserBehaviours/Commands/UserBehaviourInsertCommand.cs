using System.ComponentModel.DataAnnotations;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.UserBehaviours.Commands;

public class UserBehaviourInsertCommand : ICommand<UserBehaviourInsertResponse>
{
    [Required(ErrorMessage = "ActionType is required")]
    public ConstantEnum.UserBehaviourActionType ActionType { get; set; }
    
    public Guid? TargetId { get; set; }
    
    public ConstantEnum.UserBehaviourTargetType TargetType { get; set; }
    
    public string? Metadata { get; set; }
}

