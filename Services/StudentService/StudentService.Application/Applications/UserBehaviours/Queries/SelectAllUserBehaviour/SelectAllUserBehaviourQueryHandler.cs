using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using MediatR;
using StudentService.Domain.ReadModels;

namespace StudentService.Application.Applications.UserBehaviours.Queries.SelectAllUserBehaviour;

/// <summary>
/// Handler for getting all user behaviours
/// </summary>
public class SelectAllUserBehaviourQueryHandler(
    IQueryRepository<UserBehaviourCollection> queryRepository,
    IIdentityService identityService)
    : IRequestHandler<SelectAllUserBehaviourQuery, SelectAllUserBehaviourResponse>
{
    public async Task<SelectAllUserBehaviourResponse> Handle(SelectAllUserBehaviourQuery request, CancellationToken cancellationToken)
    {
        var response = new SelectAllUserBehaviourResponse { Success = false };
        
        // Get current user
        var currentUser = identityService.GetCurrentUser();
        
        // Build cache key with safe string concatenation
        var cacheKey = CacheKey.UserBehaviours(currentUser!.UserId);

        // Get paginated data from read model with cache
        var userBehaviours = await queryRepository.GetOrSetListAsync(
            cacheKey,
            async () =>
            {
                // Build predicate based on filters
                return await queryRepository.ToListAsync(predicate: ub => ub.StudentId == currentUser.UserId && ub.IsActive);
            },
            expiry: TimeSpan.FromMinutes(5)
        );

        // Map to DTO - Results are already ordered by CreatedAt DESC from Marten
        var dtoItems = userBehaviours
            .Select(ub => new UserBehaviourDto
            {
                Id = ub.Id,
                StudentId = ub.StudentId,
                ActionType = ub.ActionType,
                TargetId = ub.TargetId,
                TargetType = ub.TargetType,
                Metadata = ub.Metadata,
                CreatedAt = ub.CreatedAt,
            }).ToList();
        
        // True
        response.Success = true;
        response.SetMessage(MessageId.I00001, "Lấy danh sách hành vi người dùng");
        response.Response = dtoItems;
        return response;
    }
}
