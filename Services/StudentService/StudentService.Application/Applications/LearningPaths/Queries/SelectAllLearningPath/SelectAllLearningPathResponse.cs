using BaseService.Common.ApiEntities;
using BuildingBlocks.Pagination;

namespace StudentService.Application.Applications.LearningPaths.Queries.SelectAllLearningPath
{
    public record SelectAllLearningPathResponse : AbstractApiResponse<PaginatedResult<LearningPathSelectAllDto>>
    {
        public override PaginatedResult<LearningPathSelectAllDto> Response { get; set; }
            = new(pageIndex: 0, pageSize: 10, totalCount: 0, data: []);
    }

    public record LearningPathSelectAllDto
    {
        public Guid PathId { get; set; }
        public string PathName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public short Status { get; set; }
    }
}
