using AiService.Application.Features.AiSummary;
using AiService.Application.Interfaces;
using BuildingBlocks.Messaging.Events.AIService.AiFeedback;
using BuildingBlocks.Messaging.Events.AIService.ModuleProgress;
using MassTransit;
using MediatR;

namespace AiService.Application.Handler.AiSummary
{
    public class AiSummaryFeedbackModuleHandler(
        IAiSummaryService _aiSummaryService,
        IRequestClient<GetModuleProgressEvents> _moduleProgressClient,
        IRequestClient<UpdateModuleFeedbackEvent> _moduleFeedbackClient
        ) : IRequestHandler<AiSummaryFeedbackModuleRequest, AiSummaryFeedbackModuleResponse>
    {
        public async Task<AiSummaryFeedbackModuleResponse> Handle(AiSummaryFeedbackModuleRequest request, CancellationToken cancellationToken)
        {
            var @event = new GetModuleProgressEvents(request.CourseId, request.StudentId, request.ModuleId);

            var busResp = await _moduleProgressClient.GetResponse<GetModuleProgresssRepsonse>(@event, cancellationToken);

            var progress = busResp.Message.Response;

            var aiReq = new AiSummaryFeedbackModuleDto
            {
                StudentId = request.StudentId,
                CourseId = request.CourseId,
                ModuleId = request.ModuleId,
                LessonsTotal = progress.LessonsTotal,
                LessonsCompleted = progress.LessonsCompleted,
                PercentCompleted = progress.PercentCompleted,
                Score100Raw = progress.Score100Raw,
                Score100 = progress.Score100,
                Strengths = progress.Strengths,
                Improvements = progress.Improvements,
                Actions = progress.Actions,
                SkillGaps = progress.SkillGaps
            };

            var markdown = await _aiSummaryService
                .GenerateProgressFeedbackMarkdownAsync(aiReq, cancellationToken);

            // 4) Gửi event insert/update feedback module xuống service khác
            var insertEvent = new UpdateModuleFeedbackEvent(
                request.ModuleId,
                request.StudentId,
                markdown
            );

            // Nếu chỉ cần fire & chờ confirm:
            await _moduleFeedbackClient
                .GetResponse<UpdateModuleFeedbackResponse>(insertEvent, cancellationToken);

            return new AiSummaryFeedbackModuleResponse
            {
                Success = true,
                Response = markdown
            };
        }
    }
}
