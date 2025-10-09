using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.UserBehaviours.Commands;

public class UserBehaviourInsertCommandHandler(IUserBehaviourService userBehaviourService) 
    : ICommandHandler<UserBehaviourInsertCommand, UserBehaviourInsertResponse>
{
    public async Task<UserBehaviourInsertResponse> Handle(UserBehaviourInsertCommand request, CancellationToken cancellationToken)
    {
        return await userBehaviourService.InsertUserBehaviourAsync(request, cancellationToken);
    }
}

