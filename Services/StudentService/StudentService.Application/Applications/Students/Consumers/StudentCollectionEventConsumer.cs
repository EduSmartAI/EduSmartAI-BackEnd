using BaseService.Application.Interfaces.Repositories;
using MassTransit;

namespace StudentService.Application.Applications.Students.Consumers;

public class StudentCollectionEventConsumer(IUnitOfWork unitOfWork) : IConsumer<StudentCollectionEvent>
{
    public async Task Consume(ConsumeContext<StudentCollectionEvent> context)
    {
        var student = context.Message.Student;
        unitOfWork.Store(student);
        await unitOfWork.SessionSaveChangesAsync();
    }
}