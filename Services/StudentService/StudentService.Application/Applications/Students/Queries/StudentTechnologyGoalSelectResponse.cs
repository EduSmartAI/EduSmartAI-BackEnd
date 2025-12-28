using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.Students.Queries;

public record StudentTechnologyGoalSelectResponse : AbstractApiResponse<StudentTechnologyGoalSelectResponseEntity>
{
    public override StudentTechnologyGoalSelectResponseEntity Response { get; set; } = null!;
}

public class StudentTechnologyGoalSelectResponseEntity
{
    public SemesterItem Semester { get; set; } = new SemesterItem();
    
    public MajorItem Major { get; set; } = new MajorItem();
    
    public List<StudentTechnologyItem> Technologies { get; set; }

    public List<StudentLearningGoalItem> LearningGoals { get; set; }
}

public class SemesterItem
{
    public Guid? SemesterId { get; set; }
    
    public string? SemesterName { get; set; }
}

public class MajorItem
{
    public Guid? MajorId { get; set; }
    
    public string? MajorName { get; set; }
}