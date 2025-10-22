using BaseService.Application.Interfaces.Repositories;
using BuildingBlocks.Messaging.Events.CourseService;
using Microsoft.EntityFrameworkCore;

namespace Course.Application.Consumers;

public class GetCourseModuleCountEventConsumer(ICommandRepository<CourseEntity> courseRepository) : IConsumer<GetCourseModuleCountEvent>
{
    public async Task Consume(ConsumeContext<GetCourseModuleCountEvent> context)
    {
        var evt = context.Message;
        
        var totalModules = await courseRepository
            .Find(ce => ce.CourseId == evt.CourseId && ce.IsActive)
            .SelectMany(ce => ce!.Modules)
            .CountAsync();

        var response = new GetCourseModuleCountEventResponse
        {
            Success = true,
            Response = new GetCourseModuleCountEventResponseEntity
            {
                TotalModules = totalModules,
            }
        };
        
        await context.RespondAsync(response);
    }
}