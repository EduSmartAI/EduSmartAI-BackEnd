using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;

namespace PaymentService.Application.Applications.Carts.Commands.UpdateCart
{
	public record UpdateCartItemCommand(Guid CartItemId, bool? IsSelected) : ICommand<UpdateCartItemResponse>;

	public record UpdateCartItemResponse : AbstractApiResponse<string>
	{
		public override string Response { get; set; }
	}
}
