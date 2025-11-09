using Course.Application.DTOs.CoursesDTO.WishlistDTO;
using Course.Application.Wishlists.Commands.AddToWishlist;
using Course.Application.Wishlists.Commands.RemoveFromWishlist;
using Course.Application.Wishlists.Queries.GetWishlistByUserId;
using Course.Domain.ReadModels;

namespace Course.Infrastructure.Implements
{
	public sealed class WishlistService(
		IDatabase cache,
		IIdentityService _identityService,
		IUnitOfWork unitOfWork,
		ICommandRepository<CourseWishlist> wishlistCmd,
		//IQueryRepository<CourseWishlistCollection> wishlistQuery,
		ICommandRepository<CourseEntity> courseCmd,
		ICourseCache courseCache
	) : IWishlistService
	{
		/// <summary>
		/// Add Wishlist for user
		/// </summary>
		/// <param name="courseId"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<AddToWishlistResponse> AddAsync(Guid courseId, CancellationToken ct = default)
		{
			var response = new AddToWishlistResponse { Success = false};

			var user = _identityService.GetCurrentUser();

			if (user == null)
			{
				response.SetMessage(MessageId.E00000, "User not authenticated.");
				return response;
			}

			var userId = user.UserId;

			var course = await courseCmd.FirstOrDefaultAsync(x => x.CourseId == courseId && x.IsActive, ct);

			if (course == null)
			{
				response.SetMessage(MessageId.E00000, "Course not found.");
				return response;
			}

			var existingWishlistItem = await wishlistCmd.FirstOrDefaultAsync(x => x.CourseId == courseId && x.UserId == userId, ct);

			var newWishlistItem = new CourseWishlist();

			if (existingWishlistItem != null)
			{
				existingWishlistItem.IsActive = true;
				wishlistCmd.Update(existingWishlistItem, user.Email);

				unitOfWork.Store(CourseWishlistCollection.FromWriteModel(existingWishlistItem));
			}
			else
			{
				newWishlistItem.UserId = userId;
				newWishlistItem.CourseId = courseId;

				await wishlistCmd.AddAsync(newWishlistItem, user.Email);
				unitOfWork.Store(CourseWishlistCollection.FromWriteModel(newWishlistItem));
			}

			await unitOfWork.BeginTransactionAsync(async () =>
			{
				await unitOfWork.SaveChangesAsync(user.Email, ct);

				await unitOfWork.SessionSaveChangesAsync();

				return true;
			}, ct);

			response.Success = true;
			response.Response = new WishlistItemDto(existingWishlistItem != null ? existingWishlistItem.WishlistId : newWishlistItem.WishlistId, course.CourseId, course.Title, course.Slug, true, DateTime.Now);
			response.SetMessage(MessageId.I00001, "Thêm wishlist");
			return response;

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="page"></param>
		/// <param name="size"></param>
		/// <param name="search"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<GetMyWishlistResponse> GetMineAsync(int? page, int? size, string? search, CancellationToken ct = default)
		{
			var response = new GetMyWishlistResponse { Success = false };
			var user = _identityService.GetCurrentUser();
			if (user == null)
			{
				response.SetMessage(MessageId.E00000, "User not authenticated.");
				return response;
			}
			var userId = user.UserId;

			//var paged = await wishlistCmd.PagedAsync(
			//page, size,
			//predicate: x => x.UserId == user.UserId && x.IsActive)



			return response;
		}

		/// <summary>
		/// Remove From Wishlist
		/// </summary>
		/// <param name="courseId"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<RemoveFromWishlistResponse> RemoveAsync(Guid courseId, CancellationToken ct = default)
		{
			var response = new RemoveFromWishlistResponse { Success = false };
			var user = _identityService.GetCurrentUser();
			if (user == null)
			{
				response.SetMessage(MessageId.E00000, "User not authenticated.");
				return response;
			}
			var userId = user.UserId;

			var wishlistItem = await wishlistCmd.FirstOrDefaultAsync(x =>
				x.UserId == userId && x.CourseId == courseId && x.IsActive, ct);

			if (wishlistItem == null)
			{
				response.SetMessage(MessageId.E00000, $"CourseId {courseId} không tồn tại trong Wishlist của người dùng {user.Email}");
				return response;
			}

			wishlistCmd.Update(wishlistItem, user.Email, needLogicalDelete: true);
			await unitOfWork.SaveChangesAsync(ct);

			response.Success = true;
			response.Response = "removed";
			response.SetMessage(MessageId.I00001, "Đã xoá khỏi wishlist");

			return response;
		}
	}
}
