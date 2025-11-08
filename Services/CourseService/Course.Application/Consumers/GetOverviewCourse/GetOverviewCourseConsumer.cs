using BuildingBlocks.Messaging.Events.StudentService.GetOverviewCourse;
using static BaseService.Common.Utils.Const.ConstantEnum;

namespace Course.Application.Consumers.GetOverviewCourse
{
    public class GetOverviewCourseConsumer(IOverviewCourseService _overviewCourseService) : IConsumer<GetOverviewCourseEvents>
    {
        public async Task Consume(ConsumeContext<GetOverviewCourseEvents> context)
        {
            var message = context.Message;
            var type = (OverviewTypeRequest)message.type;
            switch (type)
            {
                case OverviewTypeRequest.StudentOverview:
                    {
                        var overview = await _overviewCourseService.GetOverviewCourseByIds(message, context.CancellationToken);
                        if (overview is null) return;
                        await context.RespondAsync(overview);
                        break;
                    }
                case OverviewTypeRequest.Stats:
                    {
                        var stats = await _overviewCourseService.GetCoursePaceStatsAsync(message.courseId, message.StudentId, context.CancellationToken);
                        await context.RespondAsync(new GetCoursePaceStatsResponse
                        {
                            Success = stats is not null,
                            Response = stats ?? new CoursePaceStatsDto()
                        });
                        break;
                    }
                default:
                    break;
            }
        }
    }
}
