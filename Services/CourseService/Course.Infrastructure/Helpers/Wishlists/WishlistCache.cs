using Course.Application.Interfaces.Helpers.Wishlists;

namespace Course.Infrastructure.Helpers.Wishlists
{
	public sealed class WishlistCache(IConnectionMultiplexer mux, IDatabase _cache) : IWishlistCache
	{
		private async Task DeleteByPatternAsync(string pattern)
		{
			var server = mux.GetServer(mux.GetEndPoints().FirstOrDefault()!);
			var keys = server.Keys(pattern: pattern);
			if (keys.Any()) await _cache.KeyDeleteAsync(keys.ToArray());
		}

		// Xóa toàn bộ cache wishlist của 1 user (mọi trang, mọi search)
		public async Task ClearUserWishlistAsync(Guid userId)
			=> await DeleteByPatternAsync($"Wishlist:{userId}:*");
	}
}
