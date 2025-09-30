using StudentService.Domain.ReadModels;

namespace StudentService.Application.Applications.Students.Consumers.StudentInformationUpdateds;

public class StudentInformationUpdatedEvent
{
    public StudentEvent Student { get; set; } = default!;
    
    public List<StudentTechnologyCollection> StudentTechnologies { get; set; } = default!;
    public StudentLearningGoalCollection StudentLearningGoal { get; set; }
}

public class StudentEvent
{
    public Guid StudentId { get; set; }
    public Guid MajorId { get; set; }
    public string MajorName { get; set; } = default!;
    public Guid SemesterId { get; set; }
    public string SemesterName { get; set; } = default!;
}