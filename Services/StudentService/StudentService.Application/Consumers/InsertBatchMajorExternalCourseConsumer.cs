using BuildingBlocks.Messaging.Events.AIService.UpdateExternalMajorEvent;
using MassTransit;
using StudentService.Application.Applications.LearningPathsMajor.Commands.InsertBatchLearningPathsMajor;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Consumers
{
    public class InsertBatchMajorExternalCourseConsumer(ILearningPathService service) : IConsumer<UpdateBatchExternalMajorEvent>
    {
        public async Task Consume(ConsumeContext<UpdateBatchExternalMajorEvent> context)
        {
            var evt = context.Message;

            var request = new InsertBatchLearningPathsMajorCommand
            {
                PathId = evt.LearningPathId,
                CurrentUserEmail = evt.CurrentUserEmail,
                Majors = evt.Majors.Select(m => new ExternalMajorBatchItem
                {
                    MajorCode = m.MajorCode,
                    Reason = m.Reason,
                    Steps = m.Steps?.Select(s => new StepExternalMajorBatchItem
                    {
                        Order = s.Order,
                        Title = s.Title,
                        DurationWeeks = s.DurationWeeks,
                        Objectives = s.Objectives.ToList(),
                        SuggestedCourses = s.SuggestedCourses.Select(c => new StepCourseBatchItem
                        {
                            Title = c.Title,
                            Link = c.Link,
                            Provider = c.Provider,
                            Reason = c.Reason,
                            Duration = c.Duration,
                            Level = c.Level
                        }).ToList()
                    }).ToList()
                }).ToList()
            };

            var res = await service.InsertBatchLearningPathMajorCourseAsync(request, context.CancellationToken);
            
            var resp = new UpdateBatchExternalMajorEventResponse
            {
                Success = res.Success,
                Response = res.Response,
                DetailErrors = []
            };
            
            await context.RespondAsync(resp);
        }
    }
}

