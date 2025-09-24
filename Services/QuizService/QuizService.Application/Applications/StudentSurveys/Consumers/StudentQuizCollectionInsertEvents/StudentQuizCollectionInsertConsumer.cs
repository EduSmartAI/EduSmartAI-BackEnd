using BaseService.Application.Interfaces.Repositories;
using MassTransit;

namespace QuizService.Application.Applications.StudentSurveys.Consumers.StudentQuizCollectionInsertEvents;

public class StudentQuizCollectionInsertConsumer(IUnitOfWork unitOfWork) : IConsumer<StudentQuizCollectionInsertEvent>
{
    public async Task Consume(ConsumeContext<StudentQuizCollectionInsertEvent> context)
    {
        var message = context.Message;
        var StudentQuizCollections = message.StudentQuizzes;
        foreach (var StudentQuizCollection in StudentQuizCollections)
        {
            unitOfWork.Store(StudentQuizCollection);

        }
        await unitOfWork.SessionSaveChangesAsync();
    }
}