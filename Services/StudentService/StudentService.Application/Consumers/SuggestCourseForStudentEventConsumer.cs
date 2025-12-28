using System.Text.Json;
using BaseService.Application.Interfaces.Repositories;
using BuildingBlocks.Messaging.Events.CourseService;
using MassTransit;
using StudentService.Application.Applications.SuggestCourses.Consumers;
using StudentService.Domain.ReadModels;
using StudentService.Domain.WriteModels;

namespace StudentService.Application.Consumers;

public class SuggestCourseForStudentEventConsumer(ICommandRepository<CourseSuggestion> courseSuggestionRepository,
                                                  ICommandRepository<OutboxMessage> outboxRepository,
                                                  IUnitOfWork unitOfWork) : IConsumer<SuggestCourseForStudentEvent>
{
    public async Task Consume(ConsumeContext<SuggestCourseForStudentEvent> context)
    {
        var evt = context.Message;
        
        await unitOfWork.BeginTransactionAsync(async () =>
        {
            var newSuggestions = evt.SuggestCourses.Select(s => new CourseSuggestion
            {
                StudentId = s.StudentId,
                OriginalCourseId = s.OriginalCourseId,
                SuggestedCourseId = s.SuggestedCourseId,
                Reason = s.Reason
            }).ToList();
        
            await courseSuggestionRepository.AddRangeAsync(newSuggestions);
            await unitOfWork.SaveChangesAsync(evt.SuggestCourses.First().Email, CancellationToken.None);

            var suggestCourseCollectionEvent = new SuggestCourseCollectionEvent
            {
                SuggestCourseCollections = newSuggestions.Select(CourseSuggestionCollection.FromWriteModel).ToList()
            };

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = nameof(SuggestCourseCollectionEvent),
                Content = JsonSerializer.Serialize(suggestCourseCollectionEvent),
                OccurredOnUtc = DateTime.UtcNow,
            };

            await outboxRepository.AddAsync(outboxMessage);
            await unitOfWork.SaveChangesAsync(evt.SuggestCourses.First().Email, CancellationToken.None);
            return true;
        });
    }
}