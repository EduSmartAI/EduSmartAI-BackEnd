using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;

namespace PaymentService.Application.Applications.Carts.Commands.AddToCart
{
	public record AddToCartCommand(Guid CourseId) : ICommand<AddToCartResponse>;

	public record AddToCartResponse : AbstractApiResponse<string>
	{
		public override string Response { get; set; }
	}
}
