using BuildingBlocks.CQRS;
using PaymentService.Application.Interfaces;

namespace PaymentService.Application.Applications.Carts.Commands.UpdateCart
{
	public class UpdateCartItemHandler(ICartService _cartService) : ICommandHandler<UpdateCartItemCommand, UpdateCartItemResponse>
	{
		public async Task<UpdateCartItemResponse> Handle(UpdateCartItemCommand request, CancellationToken cancellationToken)
		{
			return await _cartService.UpdateCartItemAsync(request.CartItemId, request.IsSelected, cancellationToken);
		}
	}
}
