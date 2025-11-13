using BuildingBlocks.CQRS;
using PaymentService.Application.Interfaces;

namespace PaymentService.Application.Applications.Carts.Commands.AddToCart
{
	public class AddToCartHandler(ICartService _cartService) : ICommandHandler<AddToCartCommand, AddToCartResponse>
	{
		public async Task<AddToCartResponse> Handle(AddToCartCommand request, CancellationToken cancellationToken)
		{
			return await _cartService.AddToCartAsync(request.CourseId, cancellationToken);
		}
	}
}
