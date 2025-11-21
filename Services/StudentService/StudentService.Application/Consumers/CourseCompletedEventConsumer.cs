using BuildingBlocks.Messaging.Events.CourseService;
using MassTransit;
using Microsoft.Extensions.Logging;
using StudentService.Application.Interfaces;
using static BaseService.Common.Utils.Const.ConstantEnum;

namespace StudentService.Application.Consumers
{
	public class CourseCompletedEventConsumer : IConsumer<CourseCompletedEvent>
	{
		private readonly ILearningPathService _learningPathService;
		private readonly ILogger<CourseCompletedEventConsumer> _logger;

		public CourseCompletedEventConsumer(
			ILearningPathService learningPathService,
			ILogger<CourseCompletedEventConsumer> logger)
		{
			_learningPathService = learningPathService;
			_logger = logger;
		}

		public async Task Consume(ConsumeContext<CourseCompletedEvent> context)
		{
			var message = context.Message;

			_logger.LogInformation(
				"Handling CourseCompletedEvent for User {UserId}, Course {CourseId}",
				message.UserId, message.CourseId);

			// giả sử enum CourseProgressStatus nằm trong shared lib
			await _learningPathService.UpdateCourseStatusForUserAsync(
				message.UserId,
				message.CourseId,
				(short)CourseProgressStatus.Completed,
				context.CancellationToken);
		}
	}
}
