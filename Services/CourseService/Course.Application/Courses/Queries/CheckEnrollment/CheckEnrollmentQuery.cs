using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;
using Course.Application.DTOs.CoursesDTO;

namespace Course.Application.Courses.Queries.CheckEnrollment;

public record CheckEnrollmentQuery(Guid CourseId) : IQuery<CheckEnrollmentResponse>;

public record CheckEnrollmentResponse : AbstractApiResponse<CheckEnrollmentDto>
{
	public override CheckEnrollmentDto Response { get; set; } = default!;
}


