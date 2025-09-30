using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.QuizService.StudentInformationInsertEvents;

namespace StudentService.Application.Applications.Students.Commands.Inserts;

public record StudentMajorSemesterInsertCommand : ICommand<StudentInformationMajorSemesterEventResponse>
{
    public Guid StudentId { get; set; }
    public Guid SemesterId { get; set; }
    public string SemesterName { get; set; }
    public Guid MajorId { get; set; }
    public string MajorName { get; set; }
    public List<Guid> TechnologyIds { get; set; }
    public Guid LearningGoalId { get; set; }
}

public class MajorInternal
{
    public string MajorName { get; set; } = null!;
    
    public string Reason { get; set; } = null!;
}

public class MajorExternal
{
    public string MajorName { get; set; } = null!;
    
    public string Reason { get; set; } = null!;
}