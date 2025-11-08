using AiService.Application.Features.AiSearch;
using BuildingBlocks.Messaging.Events.AIService.AiRecommend;
using MassTransit;
using MediatR;

namespace AiService.Application.Consumers.AiSearch
{
    public class SearchAiRecommendImproveConsumer(IMediator _mediator) : IConsumer<SearchAiRecommendImproveEvents>
    {
        public async Task Consume(ConsumeContext<SearchAiRecommendImproveEvents> context)
        {
            var message = context.Message;
            var request = new AiSearchRequest
            {
                topic = message.topic,
            };
            var aiSearchResult = await _mediator.Send(request, context.CancellationToken);
            var response = new SearchAiRecommendImproveResponse
            {
                Success = aiSearchResult.Success,
                Response = aiSearchResult.Response,
            };
            await context.RespondAsync(response);
        }
    }
}
