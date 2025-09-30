using BuildingBlocks.Messaging.Events.QuizService.StudentInformationInsertEvents;
using MassTransit;
using MediatR;
using StudentService.Application.Applications.Students.Commands.Inserts;

namespace StudentService.Application.Applications.Students.Consumers;

public class StudentInformationInsertConsumer(IMediator mediator) : IConsumer<StudentMajorSemesterInformationEvent>
{
    public async Task Consume(ConsumeContext<StudentMajorSemesterInformationEvent> context)
    {
        var evt = context.Message;
        
        var command = new StudentMajorSemesterInsertCommand
        {
            StudentId = evt.StudentId,
            SemesterId = evt.SemesterId,
            SemesterName = evt.SemesterName,
            MajorId = evt.MajorId,
            MajorName = evt.MajorName,
            TechnologyIds = evt.ProgramingLanguages,
            LearningGoalId = evt.LearningGoalId,
        };
        
        var response = await mediator.Send(command);
        await context.RespondAsync(response);
    }
}