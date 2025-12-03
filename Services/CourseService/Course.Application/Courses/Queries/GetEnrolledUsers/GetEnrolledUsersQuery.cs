using BuildingBlocks.Pagination;

namespace Course.Application.Courses.Queries.GetEnrolledUsers
{
	public record GetEnrolledUsersQuery(PaginationRequest Pagination, Guid CourseId) : IQuery<GetEnrolledUsersResponse>;

	public record GetEnrolledUsersResponse : AbstractApiResponse<PaginatedResult<EnrolledUsersDto>>
	{
		public override PaginatedResult<EnrolledUsersDto> Response { get ; set ; }
	}

	public class EnrolledUsersDto
	{
		public Guid UserId { get; set; }
		public string? DisplayName { get; set; }
		public string? AvatarUrl { get; set; }
	}
}
