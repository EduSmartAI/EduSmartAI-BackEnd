using BaseService.Application.Interfaces.IdentityHepers;
using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.LearningPaths.Commands.UpdateCourses;

/// <summary>
/// Handler for updating learning path courses
/// </summary>
public class LearningPathCourseUpdateCommandHandler : ICommandHandler<LearningPathCourseUpdateCommand, LearningPathCourseUpdateResponse>
{
    private readonly ILearningPathService _learningPathService;
    private readonly IIdentityService _identityService;

    public LearningPathCourseUpdateCommandHandler(
        ILearningPathService learningPathService,
        IIdentityService identityService)
    {
        _learningPathService = learningPathService;
        _identityService = identityService;
    }

    public async Task<LearningPathCourseUpdateResponse> Handle(LearningPathCourseUpdateCommand request, CancellationToken cancellationToken)
    {
        return await _learningPathService.UpdateLearningPathCoursesAsync(request, cancellationToken);
    }
}
