using BuildingBlocks.Messaging.Events.StudentInformationInsertEvents;
using MassTransit;
using MediatR;
using StudentService.Application.Applications.Students.Commands.Inserts;

namespace StudentService.Application.Applications.Students.Consumers;

public class StudentInformationInsertConsumer(IMediator mediator) : IConsumer<StudentMajorSemesterInformationEvent>
{
    public async Task Consume(ConsumeContext<StudentMajorSemesterInformationEvent> context)
    {
        var evt = context.Message;
        
        var command = new StudentMajorSemesterInsertCommand(
            SemesterId: evt.SemesterId,
            SemesterName: evt.SemesterName,
            MajorId: evt.MajorId,
            MajorName: evt.MajorName,
            StudentId: evt.StudentId,
            TechnologyIds: evt.ProgramingLanguages,
            LearningGoalIds: evt.LearningGoalIds);
        
        var response = await mediator.Send(command);
        
        await context.RespondAsync(response);
    }
}