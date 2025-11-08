using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.Dashboards.Commands
{
    public class SearchAiRecommendRequest : ICommand<SearchAiRecommendResponse>
    {
        public Guid ImprovementId { get; set; }
    }
}
