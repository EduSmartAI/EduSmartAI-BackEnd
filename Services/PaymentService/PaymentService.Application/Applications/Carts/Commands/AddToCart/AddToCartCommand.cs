using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;

namespace PaymentService.Application.Applications.Carts.Commands.AddToCart
{
	public record AddToCartCommand(Guid CourseId) : ICommand<AddToCartResponse>;

	public record AddToCartResponse : AbstractApiResponse<bool>
	{
		public override bool Response { get; set; } = new();
	}
}
