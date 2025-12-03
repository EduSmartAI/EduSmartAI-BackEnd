using BuildingBlocks.Messaging.Events.StudentService.GetStudentInformation;
using MassTransit;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Consumers
{
	public class GetStudentNameEventConsumer(IStudentService _studentService) : IConsumer<GetStudentNamesEvent>
	{
		public async Task Consume(ConsumeContext<GetStudentNamesEvent> context)
		{
			var ids = context.Message.StudentIds.Distinct().ToList();

			var studentNames = await _studentService.GetStudentNamesAsync(ids, context.CancellationToken);

			var responseEvent = new GetStudentNamesEventResponse
			{
				Success = true,
				Response = studentNames
			};

			await context.RespondAsync(responseEvent);
		}
	}
}
