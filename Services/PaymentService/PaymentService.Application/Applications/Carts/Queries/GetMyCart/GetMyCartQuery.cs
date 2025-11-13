using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;
using PaymentService.Application.DTOs.Carts;

namespace PaymentService.Application.Applications.Carts.Queries.GetMyCart
{
	public record GetMyCartQuery : ICommand<GetMyCartResponse>;

	public record GetMyCartResponse : AbstractApiResponse<CartDto>
	{
		public override CartDto Response { get; set; } = new();
	}
}
