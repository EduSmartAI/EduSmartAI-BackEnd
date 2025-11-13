using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;

namespace PaymentService.Application.Applications.Carts.Commands.UpdateCart
{
	public record UpdateCartItemCommand(Guid CartItemId, bool? IsSelected) : ICommand<UpdateCartItemResponse>;

	public record UpdateCartItemResponse : AbstractApiResponse<bool>
	{
		public override bool Response { get; set; } = new();
	}
}
