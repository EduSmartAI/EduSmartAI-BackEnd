using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.AIService.AiFeedback;
using MassTransit;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Consumers.DashboardCourse
{
    public class InsertAiFeedbackOverViewConsumer(IAiQuizEvaluateStudentService _aiQuizEvaluateStudentService) : IConsumer<InsertAiFeedbackEvents>
    {
        public async Task Consume(ConsumeContext<InsertAiFeedbackEvents> context)
        {
            var message = context.Message;
            try
            {
                var evaluationId = await _aiQuizEvaluateStudentService.InsertOverviewSummaryAsync(
                    message.StudentId,
                    message.CourseId,
                    message.markdownFeedBack,
                    context.CancellationToken);

                var resp = new InsertAiFeedbackResponse
                {
                    Success = true,
                    Response = evaluationId
                };
                resp.SetMessage(MessageId.I00001, "Lưu AI feedback overview thành công");

                await context.RespondAsync(resp);
            }
            catch (Exception ex)
            {
                var resp = new InsertAiFeedbackResponse
                {
                    Success = false,
                    Response = string.Empty
                };
                resp.SetMessage(MessageId.E99999, $"Lỗi khi lưu AI feedback overview: {ex.Message}");

                await context.RespondAsync(resp);
            }
        }
    }
}
