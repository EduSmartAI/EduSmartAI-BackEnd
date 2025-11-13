using BuildingBlocks.CQRS;
using PaymentService.Application.Interfaces;

namespace PaymentService.Application.Applications.Carts.Queries.GetMyCart
{
	public class GetMyCartHandler(ICartService _cartService) : ICommandHandler<GetMyCartQuery, GetMyCartResponse>
	{
		public async Task<GetMyCartResponse> Handle(GetMyCartQuery request, CancellationToken cancellationToken)
		{
			return await _cartService.GetMyCartAsync(cancellationToken);
		}
	}
}
