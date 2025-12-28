using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.StudentService.GetOverviewAiEvaluation;
using MassTransit;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Consumers
{
    public class GetOverviewAiEvaluationConsumer : IConsumer<GetOverviewAiEvaluationEvent>
    {
        private readonly IAiQuizEvaluateStudentService _aiQuizEvaluateService;

        public GetOverviewAiEvaluationConsumer(IAiQuizEvaluateStudentService aiQuizEvaluateService)
        {
            _aiQuizEvaluateService = aiQuizEvaluateService;
        }

        public async Task Consume(ConsumeContext<GetOverviewAiEvaluationEvent> context)
        {
            var response = new GetOverviewAiEvaluationEventResponse { Success = false };

            try
            {
                var result = await _aiQuizEvaluateService.GetOverviewAiEvaludationAsync(
                    context.Message.StudentId,
                    context.Message.CourseId,
                    context.CancellationToken);

                response.Success = true;
                response.Response = new OverviewAiEvaluationDto
                {
                    Summary = result.Summary,
                    AverageScore100Raw = result.AverageScore100Raw,
                    AverageScore100 = result.AverageScore100
                };
                response.SetMessage(MessageId.I00001, "Lấy thông tin đánh giá AI thành công");
            }
            catch (Exception ex)
            {
                response.SetMessage(MessageId.E00000, $"Lỗi khi lấy thông tin đánh giá AI: {ex.Message}");
            }

            await context.RespondAsync(response);
        }
    }
}






