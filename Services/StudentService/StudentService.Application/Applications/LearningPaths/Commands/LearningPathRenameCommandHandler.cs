using BaseService.Application.Interfaces.IdentityHepers;
using BuildingBlocks.CQRS;
using StudentService.Application.Applications.LearningPaths.Queries;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.LearningPaths.Commands;

public class LearningPathRenameCommandHandler(
    ILearningPathService learningPathService,
    IIdentityService identityService,
    ILearningPathRealtimeNotifier learningPathRealtimeNotifier)
    : ICommandHandler<LearningPathRenameCommand, LearningPathRenameResponse>
{
    public async Task<LearningPathRenameResponse> Handle(LearningPathRenameCommand request, CancellationToken cancellationToken)
    {
        var identity = identityService.GetCurrentUser();
        if (identity != null)
        {
            if (request.StudentId == Guid.Empty)
            {
                request.StudentId = identity.UserId;
            }

            if (string.IsNullOrWhiteSpace(request.StudentEmail))
            {
                request.StudentEmail = identity.Email;
            }
        }

        var response = await learningPathService.RenameLearningPathAsync(request, cancellationToken);

        if (response.Success)
        {
            var studentId = request.StudentId != Guid.Empty ? request.StudentId : identity?.UserId ?? Guid.Empty;

            if (request.LearningPathId != Guid.Empty && studentId != Guid.Empty)
            {
                var snapshot = await learningPathService.GetLearningPathById(
                    new LearningPathSelectsQuery { LearningPathId = request.LearningPathId },
                    studentId,
                    true,
                    cancellationToken);

                if (snapshot.Success)
                {
                    await learningPathRealtimeNotifier.PublishAsync(request.LearningPathId, snapshot, cancellationToken);
                }
            }
        }

        return response;
    }
}

