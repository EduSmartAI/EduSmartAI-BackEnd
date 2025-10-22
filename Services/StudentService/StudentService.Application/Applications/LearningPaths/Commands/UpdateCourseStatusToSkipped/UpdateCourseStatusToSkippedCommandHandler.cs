using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.LearningPaths.Commands.UpdateCourseStatusToSkipped;

public class UpdateCourseStatusToSkippedCommandHandler : ICommandHandler<UpdateCourseStatusToSkippedCommand, UpdateCourseStatusToSkippedResponse>
{
    private readonly ILearningPathService _learningPathService;

    public UpdateCourseStatusToSkippedCommandHandler(ILearningPathService learningPathService)
    {
        _learningPathService = learningPathService;
    }

    public async Task<UpdateCourseStatusToSkippedResponse> Handle(UpdateCourseStatusToSkippedCommand request, CancellationToken cancellationToken)
    {
        return await _learningPathService.UpdateCourseStatusToSkippedAsync(request, cancellationToken);
    }
}

