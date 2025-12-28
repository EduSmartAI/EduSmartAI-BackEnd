using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.PaymentService;

namespace Course.Application.UserLessonProgresses.Consumers
{
	public class CheckIsCourseEnrolledEventConsumer(IStudentProgressService _studentProgressService) : IConsumer<CheckIsCourseEnrolledEvent>
	{
		public async Task Consume(ConsumeContext<CheckIsCourseEnrolledEvent> context)
		{
			var evt = context.Message;
			var response = new CheckIsCourseEnrolledEventResponse { Success = false, Response = false };
			var isEnrolled = await _studentProgressService.CheckEnrollmentExternalServiceAsync(evt.CourseId, evt.UserId, context.CancellationToken);
			if (isEnrolled.Response)
			{
				response.Success = true;
				response.Response = true;
				response.SetMessage(MessageId.I00001, "Người dùng đã đăng ký khóa học");
				await context.RespondAsync(response);
				return;
			}
			response.Success = true;
			response.Response = false;
			response.SetMessage(MessageId.I00000, "Người dùng chưa đăng ký khóa học");
			await context.RespondAsync(response);
		}
	}
}
