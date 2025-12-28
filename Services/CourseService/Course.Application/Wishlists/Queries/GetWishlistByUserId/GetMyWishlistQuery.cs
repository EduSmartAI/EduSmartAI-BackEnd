using BaseService.Application.Common;
using Course.Application.DTOs.CoursesDTO.WishlistDTO;

namespace Course.Application.Wishlists.Queries.GetWishlistByUserId
{
	public record GetMyWishlistQuery(int? Page = 1, int? Size = 20, string? Search = null) : IQuery<GetMyWishlistResponse>;

	public sealed record GetMyWishlistResponse : AbstractApiResponse<PagedResult<WishlistItemDto>>
	{
		public override PagedResult<WishlistItemDto> Response { get; set; } = new();
	}
}
