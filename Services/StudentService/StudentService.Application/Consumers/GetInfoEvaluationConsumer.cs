using BuildingBlocks.Messaging.Events.StudentService.GetInfoEvaluation;
using MassTransit;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Consumers
{
    public class GetInfoEvaluationConsumer(IAiEvaluationService _aiEvaluationService) : IConsumer<GetInfoEvaluationEvent>
    {
        /// <summary>
        /// Get info AI_Evaluations
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Consume(ConsumeContext<GetInfoEvaluationEvent> context)
        {
            var message = context.Message;
            var response = await _aiEvaluationService.GetAllEvaluationByCourseId(message.StudentId, message.CourseId, context.CancellationToken);
            await context.RespondAsync(response);

        }
    }
}
