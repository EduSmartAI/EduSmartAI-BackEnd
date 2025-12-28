using BaseService.Application.Common;
using BuildingBlocks.Messaging.Events.TeacherService.GetTeacherInformation;
using Course.Application.DTOs.CoursesDTO.WishlistDTO;
using Course.Application.Interfaces.Helpers.Wishlists;
using Course.Application.Wishlists.Commands.AddToWishlist;
using Course.Application.Wishlists.Commands.RemoveFromWishlist;
using Course.Application.Wishlists.Queries.GetWishlistByUserId;
using Course.Infrastructure.Caching;

namespace Course.Infrastructure.Implements
{
	public sealed class WishlistService(
		IDatabase _cache,
		IIdentityService _identityService,
		IUnitOfWork unitOfWork,
		ICommandRepository<CourseWishlist> _wishlistCommandRepository,
		ICommandRepository<CourseEntity> _courseCommandRepository,
		IWishlistCache _wishlistCache,
		IRequestClient<GetTeacherNamesEvent> _teacherNameClient
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
			}
			else
			{
				newWishlistItem.UserId = userId;
				newWishlistItem.CourseId = courseId;

				await _wishlistCommandRepository.AddAsync(newWishlistItem, user.Email);
			}

			await unitOfWork.BeginTransactionAsync(async () =>
			{
				await unitOfWork.SaveChangesAsync(user.Email, ct);
				return true;
			}, ct);

			await _wishlistCache.ClearUserWishlistAsync(userId);

			response.Success = true;
			response.Response = true;
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

			var pageNumber = page.GetValueOrDefault(1);
			var pageSize = size.GetValueOrDefault(20);
			var searchNorm = (search ?? string.Empty).Trim().ToLower();

			var searchHash = string.IsNullOrEmpty(searchNorm) ? "0" : Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(searchNorm))).Substring(0, 16);
			var cacheKey = $"Wishlist:{user.UserId}:p{pageNumber}:s{pageSize}:q{searchHash}";
			var ttl = TimeSpan.FromMinutes(5);

			var	cached = await _cache.GetAsync<PagedResult<WishlistItemDto>>(cacheKey);
			
			if (cached is not null)
			{
				response.Success = true;
				response.Response = cached;
				response.SetMessage(MessageId.I00001, "Lấy wishlist của tôi (cache)");
				return response;
			}

			// Lấy danh sách wishlist theo trang, sort mới nhất trước, include Course
			var paged = await _wishlistCommandRepository.PagedAsync<DateTime>(
				pageNumber: page,
				pageSize: size,
				predicate: x =>
					x.UserId == userId &&
					x.IsActive &&
					(
						string.IsNullOrEmpty(searchNorm) ||
						(x.Course.Title != null && x.Course.Title.ToLower().Contains(searchNorm)) ||
						(x.Course.Slug != null && x.Course.Slug.ToLower().Contains(searchNorm))
					),
				orderBy: x => x.CreatedAt,
				orderByDescending: true,
				cancellationToken: ct,
				include: q => q.Include(w => w.Course)
			);

			var teacherIds = paged.Items.Select(w => w.Course.TeacherId).Where(id => id != Guid.Empty).Distinct().ToList();

			var teacherLookup = new Dictionary<Guid, string>();

			if (teacherIds.Count > 0)
			{
				var teacherResp = await _teacherNameClient
					.GetResponse<GetTeacherNamesEventResponse>(new GetTeacherNamesEvent(teacherIds), ct);

				teacherLookup = teacherResp.Message.Response.ToDictionary(x => x.TeacherId, x => x.DisplayName ?? "");
			}

			var wishlistItems = paged.Items.Select(w =>
			{
				teacherLookup.TryGetValue(w.Course.TeacherId, out var teacherName);

				return new WishlistItemDto(
					w.WishlistId,
					w.Course.CourseId,
					teacherName ?? "",
					w.Course.Title,
					w.Course.Description,
					w.Course.ShortDescription,
					w.Course.CourseImageUrl,
					w.Course.Level,
					w.Course.Price,
					w.Course.DealPrice,
					w.Course.Slug,
					true,
					w.CreatedAt
				);
			}).ToList();

			var result = new PagedResult<WishlistItemDto>
			{
				Items = wishlistItems,
				TotalCount = paged.TotalCount,
				PageNumber = paged.PageNumber,
				PageSize = paged.PageSize
			};

			await _cache.SetAsync(cacheKey, result, ttl);

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

			await _wishlistCache.ClearUserWishlistAsync(userId);

			response.Success = true;
			response.Response = "removed";
			response.SetMessage(MessageId.I00001, "Đã xoá khỏi wishlist");

			return response;
		}
	}
}
