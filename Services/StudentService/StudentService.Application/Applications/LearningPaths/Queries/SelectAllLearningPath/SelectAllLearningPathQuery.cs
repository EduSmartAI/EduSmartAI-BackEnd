using BuildingBlocks.CQRS;
using BuildingBlocks.Pagination;

namespace StudentService.Application.Applications.LearningPaths.Queries.SelectAllLearningPath
{
    public record SelectAllLearningPathQuery(PaginationRequest Pagination) : IQuery<SelectAllLearningPathResponse>
    {
    }
}
