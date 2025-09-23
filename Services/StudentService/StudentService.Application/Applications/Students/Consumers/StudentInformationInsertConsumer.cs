using BuildingBlocks.Messaging.Events.QuizService.StudentInformationInsertEvents;
using MassTransit;
using MediatR;
using StudentService.Application.Applications.Students.Commands.Inserts;
using StudentMajorOrientation = StudentService.Application.Applications.Students.Commands.Inserts.StudentMajorOrientation;

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
            StudentMajorOrientation = new StudentMajorOrientation
            {
                MajorExternals = evt.StudentMajorOrientation.MajorExternals.Select(x => new StudentService.Application.Applications.Students.Commands.Inserts.MajorExternal
                {
                    MajorName = x.MajorName,
                    Reason = x.Reason
                }).ToList(),
                MajorInternals = evt.StudentMajorOrientation.MajorInternals.Select(x => new StudentService.Application.Applications.Students.Commands.Inserts.MajorInternal
                {
                    MajorName = x.MajorName,
                    Reason = x.Reason
                }).ToList()
            }
        };
        
        var response = await mediator.Send(command);
        await context.RespondAsync(response);
    }
}