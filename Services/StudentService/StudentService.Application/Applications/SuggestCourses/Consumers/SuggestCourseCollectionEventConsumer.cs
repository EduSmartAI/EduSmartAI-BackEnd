using BaseService.Application.Interfaces.Repositories;
using MassTransit;
using StudentService.Domain.ReadModels;

namespace StudentService.Application.Applications.SuggestCourses.Consumers;


public class SuggestCourseCollectionEvent
{
    public List<CourseSuggestionCollection> SuggestCourseCollections { get; set; }
}

public class SuggestCourseCollectionEventConsumer(IUnitOfWork unitOfWork) : IConsumer<SuggestCourseCollectionEvent>
{
    public async Task Consume(ConsumeContext<SuggestCourseCollectionEvent> context)
    {
        var evt = context.Message;

        foreach (var courseSuggestionCollection in evt.SuggestCourseCollections)
        {
            unitOfWork.Store(courseSuggestionCollection);
        }
        await unitOfWork.SessionSaveChangesAsync();
    }
}