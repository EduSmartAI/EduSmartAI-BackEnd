using BaseService.Application.Common;
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
		ICommandRepository<CourseWishlist> _wishlistCommandRepository,
		ICommandRepository<CourseEntity> _courseCommandRepository,
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

			var course = await _courseCommandRepository.FirstOrDefaultAsync(x => x.CourseId == courseId && x.IsActive, ct);

			if (course == null)
			{
				response.SetMessage(MessageId.E00000, "Course not found.");
				return response;
			}

			var existingWishlistItem = await _wishlistCommandRepository.FirstOrDefaultAsync(x => x.CourseId == courseId && x.UserId == userId, ct);

			var newWishlistItem = new CourseWishlist();

			if (existingWishlistItem != null)
			{
				existingWishlistItem.IsActive = true;
				_wishlistCommandRepository.Update(existingWishlistItem, user.Email);

				unitOfWork.Store(CourseWishlistCollection.FromWriteModel(existingWishlistItem));
			}
			else
			{
				newWishlistItem.UserId = userId;
				newWishlistItem.CourseId = courseId;

				await _wishlistCommandRepository.AddAsync(newWishlistItem, user.Email);
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
		/// Get My Wishlist
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

			var searchLower = (search ?? string.Empty).Trim().ToLower();

			// Lấy danh sách wishlist theo trang, sort mới nhất trước, include Course
			var paged = await _wishlistCommandRepository.PagedAsync<DateTime>(
				pageNumber: page,
				pageSize: size,
				predicate: x =>
					x.UserId == userId &&
					x.IsActive &&
					(
						string.IsNullOrEmpty(searchLower) ||
						(x.Course.Title != null && x.Course.Title.ToLower().Contains(searchLower)) ||
						(x.Course.Slug != null && x.Course.Slug.ToLower().Contains(searchLower))
					),
				orderBy: x => x.CreatedAt,
				orderByDescending: true,
				cancellationToken: ct,
				include: q => q.Include(w => w.Course)
			);

			var wishlistItems = paged.Items.Select(w => new WishlistItemDto(
				w.WishlistId,
				w.Course.CourseId,
				w.Course.Title,
				w.Course.Slug,
				true,
				w.CreatedAt
			)).ToList();


			response.Success = true;
			response.Response = new PagedResult<WishlistItemDto>
			{
				Items = wishlistItems,
				TotalCount = paged.TotalCount,
				PageNumber = paged.PageNumber,
				PageSize = paged.PageSize
			};
			response.SetMessage(MessageId.I00001, "Lấy wishlist của tôi");


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

			var wishlistItem = await _wishlistCommandRepository.FirstOrDefaultAsync(x =>
				x.UserId == userId && x.CourseId == courseId && x.IsActive, ct);

			if (wishlistItem == null)
			{
				response.SetMessage(MessageId.E00000, $"CourseId {courseId} không tồn tại trong Wishlist của người dùng {user.Email}");
				return response;
			}

			_wishlistCommandRepository.Update(wishlistItem, user.Email, needLogicalDelete: true);
			await unitOfWork.SaveChangesAsync(ct);

			response.Success = true;
			response.Response = "removed";
			response.SetMessage(MessageId.I00001, "Đã xoá khỏi wishlist");

			return response;
		}
	}
}
