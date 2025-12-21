using BuildingBlocks.CQRS;
using StudentService.Application.Applications.LearningPaths.Commands.ExportSubjectMarkUpdateWord;

namespace StudentService.Application.Applications.LearningPaths.Commands.ExportSubjectMarkUpdateWord;

public class ExportSubjectMarkUpdateWordCommand : ICommand<ExportSubjectMarkUpdateWordResponse>
{
    public required List<SubjectMarkUpdateDto> SubjectMarkUpdates { get; set; }
    public string? Title { get; set; }
    public string? StudentName { get; set; }
}

public class SubjectMarkUpdateDto
{
    public required string SubjectCode { get; set; }
    public required string SubjectName { get; set; }
    public double? OldMark { get; set; }
    public required double NewMark { get; set; }
    public double? MarkImprovement { get; set; }
    public required string ImprovementAnalysis { get; set; }
    public string? ComparisonAnalysis { get; set; }
}

