using BuildingBlocks.CQRS;

namespace AiService.Application.Features.AiSearch
{
    public class AiSearchRequest : ICommand<AiSearchResponse>
    {
        public string topic { get; set; } = string.Empty;
    }
}
