using BuildingBlocks.Messaging.Events.StudentService.GetAllDetailCourse;

namespace Course.Application.Consumers.GetInfoCourse
{
    public class GetInfoCourseByCourseIdConsumer(IVwCourseInforService _courseService) : IConsumer<GetAllDetailCourseEvent>
    {
        /// <summary>
        /// Get more info related course id
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Consume(ConsumeContext<GetAllDetailCourseEvent> context)
        {
            var message = context.Message;
            var dto = await _courseService.GetAllInfoByCourseId(message.CourseId, message.StudentId, context.CancellationToken);
            await context.RespondAsync<GetAllDetailCourseResponse>(
            new GetAllDetailCourseResponse
            {
                Response = dto,
                Success = true
            });
        }
    }
}
