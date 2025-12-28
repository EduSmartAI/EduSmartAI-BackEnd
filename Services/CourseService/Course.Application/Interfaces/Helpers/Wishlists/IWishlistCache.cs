namespace Course.Application.Interfaces.Helpers.Wishlists
{
	public interface IWishlistCache
	{
		Task ClearUserWishlistAsync(Guid userId);
	}
}
