using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;

namespace PaymentService.Application.Applications.Carts.Commands.RemoveCart
{
	public record RemoveCartItemCommand(Guid CartItemId) : ICommand<RemoveCartItemResponse>;

	public record RemoveCartItemResponse : AbstractApiResponse<bool>
	{
		public override bool Response { get; set; } = new();
	}
}
