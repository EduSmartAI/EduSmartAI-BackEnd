using MediatR;

namespace StudentService.Application.Applications.UserBehaviours.Queries.SelectAllUserBehaviour;

/// <summary>
/// Query to get all user behaviours for current user
/// </summary>
public class SelectAllUserBehaviourQuery : IRequest<SelectAllUserBehaviourResponse>
{
}