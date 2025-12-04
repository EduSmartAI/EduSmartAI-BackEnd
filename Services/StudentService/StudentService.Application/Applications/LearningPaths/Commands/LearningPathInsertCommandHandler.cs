using BaseService.Application.Interfaces.IdentityHepers;
using BuildingBlocks.CQRS;
using StudentService.Application.Applications.LearningPaths.Queries;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.LearningPaths.Commands;

public class LearningPathInsertCommandHandler(
    ILearningPathService learningPathService,
    IIdentityService identityService,
    ILearningPathRealtimeNotifier learningPathRealtimeNotifier)
    : ICommandHandler<LearningPathInsertSteamCommand, LearningPathInsertResponse>
{
    public async Task<LearningPathInsertResponse> Handle(LearningPathInsertSteamCommand request, CancellationToken cancellationToken)
    {
        var identity = identityService.GetCurrentUser();

        if (request.PathId == Guid.Empty)
        {
            request.PathId = Guid.NewGuid();
        }
        var command = new LearningPathInsertCommand
        {
            PathId = request.PathId,
            PathName = request.PathName,
            StudentEmail = identity!.Email,
            StudentId = identity!.UserId,
        };

        var response = await learningPathService.InsertLearningPathAsync(command, cancellationToken);

        if (response.Success)
        {
            if (string.IsNullOrWhiteSpace(response.Response))
            {
                response.Response = request.PathId.ToString();
            }

            var studentId = identity?.UserId ?? Guid.Empty;

            if (request.PathId != Guid.Empty && studentId != Guid.Empty)
            {
                var snapshot = await learningPathService.GetLearningPathById(
                    new LearningPathSelectsQuery { LearningPathId = request.PathId },
                    studentId,
                    true,
                    cancellationToken);

                if (snapshot.Success)
                {
                    await learningPathRealtimeNotifier.PublishAsync(request.PathId, snapshot, cancellationToken);
                }
            }
        }

        return response;
    }
}

