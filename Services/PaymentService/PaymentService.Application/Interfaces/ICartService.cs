using PaymentService.Application.Applications.Carts.Commands.AddToCart;
using PaymentService.Application.Applications.Carts.Commands.RemoveCart;
using PaymentService.Application.Applications.Carts.Queries.CheckCourseInMyCart;
using PaymentService.Application.Applications.Carts.Queries.GetMyCart;

namespace PaymentService.Application.Interfaces
{
	public interface ICartService
	{
		Task<GetMyCartResponse> GetMyCartAsync(CancellationToken ct = default);

		Task<AddToCartResponse> AddToCartAsync(Guid courseId, CancellationToken ct = default);
		
		Task<RemoveCartItemResponse> RemoveCartItemAsync(Guid cartItemId, CancellationToken ct = default);

		Task<CheckCourseInCartResponse> CheckCourseInMyCartAsync(Guid courseId, CancellationToken ct = default);
	}
}
