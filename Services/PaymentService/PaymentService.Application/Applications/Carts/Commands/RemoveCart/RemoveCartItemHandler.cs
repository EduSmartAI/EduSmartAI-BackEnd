using BuildingBlocks.CQRS;
using PaymentService.Application.Interfaces;

namespace PaymentService.Application.Applications.Carts.Commands.RemoveCart
{
	public class RemoveCartItemHandler(ICartService _cartService) : ICommandHandler<RemoveCartItemCommand, RemoveCartItemResponse>
	{
		public async Task<RemoveCartItemResponse> Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
		{
			return await _cartService.RemoveCartItemAsync(request.CartItemId, cancellationToken);
		}
	}
}
