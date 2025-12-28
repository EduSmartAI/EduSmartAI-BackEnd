using BuildingBlocks.CQRS;

namespace AiService.Application.Features.AiSearch
{
    public class AiSearchChatBotRequest : ICommand<AiSearchResponse>
    {
        public string topic { get; set; } = string.Empty;
        public int difficultyLevel { get; set; }
    }
}
