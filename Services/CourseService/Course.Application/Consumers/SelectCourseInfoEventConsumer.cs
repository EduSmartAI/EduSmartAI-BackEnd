using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.CourseService;
using Microsoft.EntityFrameworkCore;

namespace Course.Application.Consumers;

public class SelectCourseInfoEventConsumer(ICommandRepository<CourseEntity> repository) : IConsumer<SelectCourseInfoEvent>
{
    public async Task Consume(ConsumeContext<SelectCourseInfoEvent> context)
    {
        var response = new SelectCourseInfoEventResponse { Success = false };
        var evt = context.Message;

        var courseSelect = await repository
            .Find(x => x.CourseId == evt.CourseId && x.IsActive,
                includes: x => x.Subject)
            .Select(x => new SelectCourseInfoEventResponseEntity
            {
                CourseId = x.CourseId,
                Title = x.Title,
                SubjectCode = x.Subject.SubjectCode,
                ImageUrl = x.CourseImageUrl,
                Level = x.Level ?? 1,
                Price = x.Price,
                DealPrice = x.DealPrice ?? x.Price
            }).FirstOrDefaultAsync();
        if (courseSelect == null)
        {
            response.SetMessage(MessageId.I00000, "Không tìm thấy khóa học");
            await context.RespondAsync(response);
            return;
        }
        
        // True
        response.Success = true;
        response.Response = courseSelect;
        response.SetMessage(MessageId.I00001, "Lấy thông tin khóa học");
        await context.RespondAsync(response);
    }
}