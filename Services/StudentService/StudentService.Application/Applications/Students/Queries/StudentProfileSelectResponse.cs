using BaseService.Common.ApiEntities;
using BaseService.Common.Utils.Const;

namespace StudentService.Application.Applications.Students.Queries;

public record StudentProfileSelectResponse : AbstractApiResponse<StudentProfileSelectResponseEntity>
{
    public override StudentProfileSelectResponseEntity Response { get; set; } = null!;
}

public class StudentProfileSelectResponseEntity
{
    public Guid StudentId { get; set; }
    
    public string FirstName { get; set; } = null!;
    
    public string LastName { get; set; } = null!;
    
    public string FullName => $"{FirstName} {LastName}";
    
    public DateOnly? DateOfBirth { get; set; }
    
    public string? PhoneNumber { get; set; }
    
    public short? Gender { get; set; }
    
    public string? AvatarUrl { get; set; }
    
    public string? Address { get; set; }
    
    public string? Bio { get; set; }
    
    public Guid? MajorId { get; set; }
    
    public string? MajorName { get; set; }
    
    public Guid? SemesterId { get; set; }
    
    public string? SemesterName { get; set; }
    
    public List<StudentTechnologyItem>? Technologies { get; set; }
    
    public List<StudentLearningGoalItem>? LearningGoals { get; set; }
}

public class StudentTechnologyItem
{
    public Guid TechnologyId { get; set; }
    
    public string TechnologyName { get; set; } = null!;
    
    public short TechnologyType { get; set; }
    
    public string TechnologyTypeName { get; set; } = null!;
}

public class StudentLearningGoalItem
{
    public Guid GoalId { get; set; }
    
    public string GoalName { get; set; } = null!;
    
    public short LearningGoalType { get; set; }
    
    public string LearningGoalTypeName { get; set; } = null!;
}

