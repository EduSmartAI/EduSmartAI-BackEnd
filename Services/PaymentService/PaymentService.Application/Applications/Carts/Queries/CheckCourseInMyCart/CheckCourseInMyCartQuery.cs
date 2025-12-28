using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;
using PaymentService.Application.DTOs.Carts;

namespace PaymentService.Application.Applications.Carts.Queries.CheckCourseInMyCart
{
	public record CheckCourseInMyCartQuery(Guid CourseId) : IQuery<CheckCourseInCartResponse>;

	public record CheckCourseInCartResponse : AbstractApiResponse<CheckCourseInCartDto>
	{
		public override CheckCourseInCartDto Response { get; set; } = new();
	}
}
