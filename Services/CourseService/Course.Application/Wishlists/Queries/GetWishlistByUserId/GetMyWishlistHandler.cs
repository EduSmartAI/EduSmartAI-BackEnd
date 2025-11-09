namespace Course.Application.Wishlists.Queries.GetWishlistByUserId
{
	public sealed class GetMyWishlistHandler(IWishlistService _wishlistService) : IQueryHandler<GetMyWishlistQuery, GetMyWishlistResponse>
	{
		public async Task<GetMyWishlistResponse> Handle(GetMyWishlistQuery request, CancellationToken cancellationToken)
			=> await _wishlistService.GetMineAsync(request.Page, request.Size, request.Search, cancellationToken);
	}
}
