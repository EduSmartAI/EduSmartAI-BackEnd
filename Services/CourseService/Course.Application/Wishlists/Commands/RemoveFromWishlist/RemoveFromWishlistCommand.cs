using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Course.Application.Wishlists.Commands.RemoveFromWishlist
{
	public record RemoveFromWishlistCommand(Guid CourseId) : ICommand<RemoveFromWishlistResponse>;

	public sealed record RemoveFromWishlistResponse : AbstractApiResponse<string>
	{
		public override string Response { get; set; } = string.Empty;
	}
}
