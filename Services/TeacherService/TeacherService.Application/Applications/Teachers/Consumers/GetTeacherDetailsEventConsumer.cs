using BuildingBlocks.Messaging.Events.TeacherService.GetTeacherInformation;
using MassTransit;
using TeacherService.Application.Interfaces;

namespace TeacherService.Application.Applications.Teachers.Consumers
{
	public class GetTeacherDetailsEventConsumer(ITeacherService _teacherService) : IConsumer<GetTeacherDetailsEvent>
	{
		public async Task Consume(ConsumeContext<GetTeacherDetailsEvent> context)
		{
			var evt = context.Message;
			// Logic to get teacher details by evt.TeacherId
			var teacherDetails = await _teacherService.GetTeacherDetailAsync(evt.TeacherId, context.CancellationToken);
			var response = new GetTeacherDetailsEventResponse
			{
				Success = teacherDetails.Success,
				Message = teacherDetails.Message
			};
			if (teacherDetails.Success && teacherDetails.Response is not null)
			{
				response.Response = new TeacherDetailExternalServiceDto
				{
					TeacherId = teacherDetails.Response.TeacherId,
					DisplayName = teacherDetails.Response.DisplayName,
					FirstName = teacherDetails.Response.FirstName,
					LastName = teacherDetails.Response.LastName,
					Bio = teacherDetails.Response.Bio,
					ProfilePictureUrl = teacherDetails.Response.ProfilePictureUrl
				};
			}
			await context.RespondAsync(response);
		}
	}
}
