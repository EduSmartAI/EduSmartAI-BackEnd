using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;

namespace PaymentService.Application.Applications.Carts.Commands.RemoveCart
{
	public record RemoveCartItemCommand(Guid CartItemId) : ICommand<RemoveCartItemResponse>;

	public record RemoveCartItemResponse : AbstractApiResponse<string>
	{
		public override string Response { get; set; }
	}
}
