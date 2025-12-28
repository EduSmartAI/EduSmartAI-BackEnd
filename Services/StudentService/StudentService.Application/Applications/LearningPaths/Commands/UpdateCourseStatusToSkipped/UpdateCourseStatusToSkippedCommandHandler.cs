using BaseService.Application.Interfaces.IdentityHepers;
using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.LearningPaths.Commands.UpdateCourseStatusToSkipped;

public class UpdateCourseStatusToSkippedCommandHandler : ICommandHandler<UpdateCourseStatusToSkippedCommand, UpdateCourseStatusToSkippedResponse>
{
    private readonly ILearningPathService _learningPathService;
    private readonly IIdentityService _identityService;

    public UpdateCourseStatusToSkippedCommandHandler(ILearningPathService learningPathService, IIdentityService identityService)
    {
        _learningPathService = learningPathService;
        _identityService = identityService;
    }

    public async Task<UpdateCourseStatusToSkippedResponse> Handle(UpdateCourseStatusToSkippedCommand request, CancellationToken cancellationToken)
    {
        return await _learningPathService.UpdateCourseStatusToSkippedAsync(request, _identityService.GetCurrentUser()!.UserId, _identityService.GetCurrentUser()!.Email, cancellationToken);
    }
}

