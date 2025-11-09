using Course.Application.Wishlists.Commands.AddToWishlist;
using Course.Application.Wishlists.Commands.RemoveFromWishlist;
using Course.Application.Wishlists.Queries.GetWishlistByUserId;

namespace Course.Application.Interfaces
{
	public interface IWishlistService
	{
		Task<AddToWishlistResponse> AddAsync(Guid courseId, CancellationToken ct = default);
		Task<RemoveFromWishlistResponse> RemoveAsync(Guid courseId, CancellationToken ct = default);
		Task<GetMyWishlistResponse> GetMineAsync(int? page, int? size, string? search, CancellationToken ct = default);
	}
}
