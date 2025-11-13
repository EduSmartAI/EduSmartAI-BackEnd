using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Applications.Carts.Commands.AddToCart;
using PaymentService.Application.Applications.Carts.Commands.RemoveCart;
using PaymentService.Application.Applications.Carts.Commands.UpdateCart;
using PaymentService.Application.Applications.Carts.Queries.GetMyCart;
using PaymentService.Application.DTOs.Carts;
using PaymentService.Application.Interfaces;
using PaymentService.Domain.Models;
using static BaseService.Common.Utils.Const.ConstantEnum;
using static PaymentService.Infrastructure.Common.Helpers.CartHelper;

namespace PaymentService.Infrastructure.Implements
{
	public class CartService(
		IIdentityService _identityService,
		IUnitOfWork _unitOfWork,
		ICommandRepository<Cart> _cartRepository,
		ICommandRepository<CartItem> _cartItemRepository
	) : ICartService
	{
		public async Task<AddToCartResponse> AddToCartAsync(Guid courseId, CancellationToken ct = default)
		{
			var response = new AddToCartResponse { Success = false };

			var currentUser = _identityService.GetCurrentUser();
			if (currentUser is null)
			{
				response.SetMessage(MessageId.E00000, "Người dùng chưa đăng nhập");
				return response;
			}

			// Lấy hoặc tạo cart Active
			var cart = await _cartRepository
				.Find(x => x.UserId == currentUser.UserId &&
						   x.Status == (short)CartStatus.Active,
					isTracking: true, ct)
				.FirstOrDefaultAsync(ct);

			if (cart is null)
			{
				cart = new Cart
				{
					UserId = currentUser.UserId,
					Status = (short)CartStatus.Active,
					CreatedBy = currentUser.Email,
					UpdatedBy = currentUser.Email,
					CreatedAt = DateTime.UtcNow,
					UpdatedAt = DateTime.UtcNow
				};

				await _cartRepository.AddAsync(cart);
				await _unitOfWork.SaveChangesAsync(ct);
			}

			// Check item đã tồn tại chưa
			var existingItem = await _cartItemRepository
				.Find(x => x.CartId == cart.CartId &&
						   x.CourseId == courseId &&
						   x.Status == (short)CartItemStatus.Active,
					isTracking: false, ct)
				.FirstOrDefaultAsync(ct);

			if (existingItem is not null)
			{
				response.SetMessage(MessageId.E00000, "Khóa học đã có trong giỏ hàng");
				return response;
			}

			// Gọi Course Service lấy thông tin snapshot
			//var courseInfo = await _courseCatalogGateway.GetCourseForCartAsync(courseId, ct);
			//if (courseInfo is null)
			//{
			//	response.SetMessage(MessageId.E00000, "Không tìm thấy khóa học");
			//	return response;
			//}

			//var cartItem = new CartItem
			//{
			//	CartId = cart.CartId,
			//	CourseId = courseId,
			//	CourseTitleSnapshot = courseInfo.Title ?? null,
			//	CourseImageUrlSnapshot = courseInfo.ImageUrl ?? null,
			//	PriceSnapshot = courseInfo.Price ?? 0,
			//	DealPriceSnapshot = courseInfo.DealPrice ?? 0,
			//	IsSelected = true,
			//	Status = (short)CartItemStatus.Active,
			//	CreatedBy = currentUser.Email,
			//	UpdatedBy = currentUser.Email,
			//	CreatedAt = DateTime.UtcNow,
			//	UpdatedAt = DateTime.UtcNow
			//};

			var cartItem = new CartItem
			{
				CartId = cart.CartId,
				CourseId = courseId,
				CourseTitleSnapshot = "Title",
				CourseImageUrlSnapshot = "ImageUrl",
				PriceSnapshot = 100,
				DealPriceSnapshot = 50,
				IsSelected = true,
				Status = (short)CartItemStatus.Active,
				CreatedBy = currentUser.Email,
				UpdatedBy = currentUser.Email,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			await _cartItemRepository.AddAsync(cartItem);
			await _unitOfWork.SaveChangesAsync(ct);

			response.Success = true;
			response.SetMessage(MessageId.I00001, "Thêm khóa học vào giỏ hàng");

			return response;
		}

		/// <summary>
		/// Get my cart
		/// </summary>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<GetMyCartResponse> GetMyCartAsync(CancellationToken ct = default)
		{
			var response = new GetMyCartResponse { Success = false };

			var currentUser = _identityService.GetCurrentUser();
			if (currentUser is null)
			{
				response.SetMessage(MessageId.E00000, "Người dùng chưa đăng nhập");
				return response;
			}

			// Lấy cart Active
			var cart = await _cartRepository
				.Find(x => x.UserId == currentUser.UserId &&
						   x.Status == (short)CartStatus.Active,
					isTracking: false, ct)
				.FirstOrDefaultAsync(ct);

			if (cart is null)
			{
				// Không có cart -> trả giỏ rỗng
				response.Success = true;
				response.Response = new CartDto
				{
					CartId = Guid.Empty,
					UserId = currentUser.UserId,
					Items = new List<CartItemDto>()
				};
				response.SetMessage(MessageId.I00001, "Giỏ hàng trống");
				return response;
			}

			var items = await _cartItemRepository
				.Find(x => x.CartId == cart.CartId &&
						   x.Status == (short)CartItemStatus.Active,
					isTracking: false, ct)
				.ToListAsync(ct);

			response.Response = new CartDto
			{
				CartId = cart.CartId,
				UserId = cart.UserId,
				Items = items.Select(MapToDto).ToList()
			};

			response.Success = true;
			response.SetMessage(MessageId.I00001, "Lấy giỏ hàng thành công");
			return response;
		}

		public async Task<RemoveCartItemResponse> RemoveCartItemAsync(Guid cartItemId, CancellationToken ct = default)
		{
			var response = new RemoveCartItemResponse { Success = false };

			var currentUser = _identityService.GetCurrentUser();
			if (currentUser is null)
			{
				response.SetMessage(MessageId.E00000, "Người dùng chưa đăng nhập");
				return response;
			}

			var cart = await _cartRepository
				.Find(x => x.UserId == currentUser.UserId &&
						   x.Status == (short)CartStatus.Active,
					isTracking: false, ct)
				.FirstOrDefaultAsync(ct);

			if (cart is null)
			{
				response.SetMessage(MessageId.E00000, "Không tìm thấy giỏ hàng");
				return response;
			}

			var item = await _cartItemRepository
				.Find(x => x.CartItemId == cartItemId &&
						   x.CartId == cart.CartId &&
						   x.Status == (short)CartItemStatus.Active,
					isTracking: true, ct)
				.FirstOrDefaultAsync(ct);

			if (item is null)
			{
				response.SetMessage(MessageId.E00000, "Không tìm thấy item trong giỏ hàng");
				return response;
			}

			item.Status = (short)CartItemStatus.Removed;

			// Nếu bạn muốn logical delete luôn (IsActive = false)
			_cartItemRepository.Update(item);

			await _unitOfWork.SaveChangesAsync(ct);

			response.Success = true;
			response.Response = true;
			response.SetMessage(MessageId.I00001, "Xóa item khỏi giỏ hàng");
			return response;
		}

		/// <summary>
		/// Update cart item
		/// </summary>
		/// <param name="cartItemId"></param>
		/// <param name="isSelected"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<UpdateCartItemResponse> UpdateCartItemAsync(Guid cartItemId, bool? isSelected, CancellationToken ct = default)
		{
			var response = new UpdateCartItemResponse { Success = false };

			var currentUser = _identityService.GetCurrentUser();
			if (currentUser is null)
			{
				response.SetMessage(MessageId.E00000, "Người dùng chưa đăng nhập");
				return response;
			}

			// Lấy cart active của user
			var cart = await _cartRepository
				.Find(x => x.UserId == currentUser.UserId &&
						   x.Status == (short)CartStatus.Active,
					isTracking: false, ct)
				.FirstOrDefaultAsync(ct);

			if (cart is null)
			{
				response.SetMessage(MessageId.E00000, "Không tìm thấy giỏ hàng");
				return response;
			}

			var item = await _cartItemRepository
				.Find(x => x.CartItemId == cartItemId &&
						   x.CartId == cart.CartId &&
						   x.Status == (short)CartItemStatus.Active,
					isTracking: true, ct)
				.FirstOrDefaultAsync(ct);

			if (item is null)
			{
				response.SetMessage(MessageId.E00000, "Không tìm thấy item trong giỏ hàng");
				return response;
			}

			if (isSelected.HasValue)
			{
				item.IsSelected = isSelected.Value;
				item.UpdatedBy = currentUser.Email;
				item.UpdatedAt = DateTime.UtcNow;
			}

			_cartItemRepository.Update(item);
			await _unitOfWork.SaveChangesAsync(ct);

			response.Success = true;
			response.Response = true;
			response.SetMessage(MessageId.I00001, "Cập nhật giỏ hàng");
			return response;
		}
	}
}
