using StudentService.Application.Applications.UserBehaviours.Commands;

namespace StudentService.Application.Interfaces;

public interface IUserBehaviourService
{
    Task<UserBehaviourInsertResponse> InsertUserBehaviourAsync(UserBehaviourInsertCommand request, CancellationToken cancellationToken);
}

