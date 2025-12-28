namespace Course.Application.Wishlists.Commands.RemoveFromWishlist
{
	public sealed class RemoveFromWishlistHandler(IWishlistService _wishlistService) : ICommandHandler<RemoveFromWishlistCommand, RemoveFromWishlistResponse>
	{
		public async Task<RemoveFromWishlistResponse> Handle(RemoveFromWishlistCommand request, CancellationToken cancellationToken)
			=> await _wishlistService.RemoveAsync(request.CourseId, cancellationToken);
	}
}
