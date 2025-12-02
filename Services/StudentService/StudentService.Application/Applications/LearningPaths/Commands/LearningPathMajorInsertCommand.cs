using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.QuizService;

namespace StudentService.Application.Applications.LearningPaths.Commands;

public class LearningPathMajorInsertCommand : ICommand<LearningPathMajorInternalInsertResponse>
{
    public required string CurrentUserEmail { get; set; }
    public required Guid LearningPathId { get; set; }
    public required List<LearningPathMajorRequest> Majors { get; set; }
    public required short MajorType { get; set; }
    public required int LimitTime { get; set; }
    public required Guid SemesterId { get; set; }
    public required short StudentLevel { get; set; }
    public required StudentMajor StudentMajor { get; set; }
    public List<string>? StudentPassedSubjects { get; set; }
    public required List<CourseImprove>? CourseImproves { get; set; }
}

public class LearningPathMajorRequest
{
    public string MajorCode { get; set; }
    
    public string Reason { get; set; }
}

public class CourseImprove
{
    public string SubjectCode { get; set; } = null!;
    
    public string? SubjectPrerequisiteCode { get; set; }

    public int Level { get; set; }
}