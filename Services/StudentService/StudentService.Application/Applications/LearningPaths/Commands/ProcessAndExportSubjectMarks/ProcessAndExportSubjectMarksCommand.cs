using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.LearningPaths.Commands.ProcessAndExportSubjectMarks;

public record ProcessAndExportSubjectMarksCommand : ICommand<ProcessAndExportSubjectMarksResponse>
{
    public required Guid LearningPathId { get; init; }
}

