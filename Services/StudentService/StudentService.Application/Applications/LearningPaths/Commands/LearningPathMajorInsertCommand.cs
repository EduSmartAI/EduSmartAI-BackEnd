using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.LearningPaths.Commands;

public class LearningPathMajorInsertCommand : ICommand<LearningPathMajorInternalInsertResponse>
{
    public string CurrentUserEmail { get; set; }
    
    public Guid LearningPathId { get; set; }
    
    public List<LearningPathMajorRequest> Majors { get; set; }
    
    public short MajorType { get; set; }
    
    public int LimitTime { get; set; }
    public Guid SemesterId { get; set; }
    public short StudentLevel { get; set; }
    
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