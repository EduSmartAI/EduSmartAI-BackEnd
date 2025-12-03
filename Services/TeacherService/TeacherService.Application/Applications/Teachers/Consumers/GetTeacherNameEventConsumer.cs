using BuildingBlocks.Messaging.Events.TeacherService.GetTeacherInformation;
using MassTransit;
using TeacherService.Application.Interfaces;

namespace TeacherService.Application.Applications.Teachers.Consumers
{
	public class GetTeacherNameEventConsumer(ITeacherService _teacherService) : IConsumer<GetTeacherNamesEvent>
	{
		public async Task Consume(ConsumeContext<GetTeacherNamesEvent> context)
		{
			// Logic to get teacher name by evt.TeacherId
			var ids = context.Message.TeacherIds.Distinct().ToList();

			var list = await _teacherService.GetTeacherNamesAsync(ids, context.CancellationToken);

			var response = new GetTeacherNamesEventResponse
			{
				Success = true,
				Response = list
			};

			await context.RespondAsync(response);
		}
	}
}
