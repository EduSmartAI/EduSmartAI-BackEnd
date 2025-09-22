using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;

namespace Course.Application.Courses.Queries.CheckEnrollment;

public record CheckEnrollmentQuery(Guid CourseId) : IQuery<CheckEnrollmentResponse>;

public record CheckEnrollmentResponse : AbstractApiResponse<bool>
{
	public override bool Response { get; set; } = default!;
}