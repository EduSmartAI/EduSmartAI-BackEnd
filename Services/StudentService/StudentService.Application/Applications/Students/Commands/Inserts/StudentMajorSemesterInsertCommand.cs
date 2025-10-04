using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.QuizService;

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