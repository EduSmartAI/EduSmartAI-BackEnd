using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.StudentInformationInsertEvents;

namespace StudentService.Application.Applications.Students.Commands.Inserts;

public record StudentMajorSemesterInsertCommand(Guid StudentId, Guid SemesterId, string SemesterName,
    Guid MajorId, string MajorName, List<Guid> TechnologyIds, List<Guid> LearningGoalIds) : ICommand<StudentInformationMajorSemesterEventResponse>;