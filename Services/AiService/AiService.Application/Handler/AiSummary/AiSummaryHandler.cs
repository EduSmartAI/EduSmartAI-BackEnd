using AiService.Application.Features.AiSummary;
using AiService.Application.Interfaces;
using BuildingBlocks.Messaging.Events.AIService.AiFeedback;
using MassTransit;
using MediatR;

namespace AiService.Application.Handler.AiSummary
{
    public class AiSummaryHandler(IAiSummaryService _aiSummaryService, IRequestClient<InsertAiFeedbackEvents> _courseOverviewClient)
        : IRequestHandler<AiSummaryRequest, AiSummaryResponse>
    {
        public async Task<AiSummaryResponse> Handle(AiSummaryRequest request, CancellationToken cancellationToken)
        {
            var responseAI = await _aiSummaryService.FeedBackCourseByAI(request, cancellationToken);
            var @event = new InsertAiFeedbackEvents(request.CourseId, request.StudentId, responseAI.Response);
            var busResponse = await _courseOverviewClient.GetResponse<InsertAiFeedbackResponse>(@event, cancellationToken);
            var result = new AiSummaryResponse
            {
                Success = busResponse.Message.Success,
                Response = busResponse.Message.Response
            };

            return result;
        }
    }
}
