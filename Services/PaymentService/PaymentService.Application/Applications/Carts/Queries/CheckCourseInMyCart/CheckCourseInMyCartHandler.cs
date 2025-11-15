using BuildingBlocks.CQRS;
using PaymentService.Application.Interfaces;

namespace PaymentService.Application.Applications.Carts.Queries.CheckCourseInMyCart
{
	public class CheckCourseInMyCartHandler(ICartService _cartService) : IQueryHandler<CheckCourseInMyCartQuery, CheckCourseInCartResponse>
	{
		public async Task<CheckCourseInCartResponse> Handle(CheckCourseInMyCartQuery request, CancellationToken cancellationToken)
		{
			return await _cartService.CheckCourseInMyCartAsync(request.CourseId, cancellationToken);
		}
	}
}
