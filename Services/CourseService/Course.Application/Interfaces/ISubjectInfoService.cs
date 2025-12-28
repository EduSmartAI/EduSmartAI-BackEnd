using BuildingBlocks.Messaging.Events.AIService.SubjectInfoEvent;

namespace Course.Application.Interfaces;

public interface ISubjectInfoService
{
    Task<IReadOnlyList<SubjectInfoItem>> GetByMajorAsync(string majorCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<SubjectInfoItem>> GetBySubjectCodesAsync(
        IReadOnlyCollection<string> subjectCodes,
        CancellationToken cancellationToken);
}

