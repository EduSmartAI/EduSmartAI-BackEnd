using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.CourseService;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Applications.Carts.Commands.AddToCart;
using PaymentService.Application.Applications.Carts.Commands.RemoveCart;
using PaymentService.Application.Applications.Carts.Commands.UpdateCart;
using PaymentService.Application.Applications.Carts.Queries.CheckCourseInMyCart;
using PaymentService.Application.Applications.Carts.Queries.GetMyCart;
using PaymentService.Application.DTOs.Carts;
using PaymentService.Application.Interfaces;
using PaymentService.Domain.WriteModels;
using static BaseService.Common.Utils.Const.ConstantEnum;
using static PaymentService.Infrastructure.Common.Helpers.CartHelper;

namespace PaymentService.Infrastructure.Implements
{
	public class CartService(
		IIdentityService identityService,
		IUnitOfWork unitOfWork,
		ICommandRepository<Cart> cartRepository,
		ICommandRepository<CartItem> cartItemRepository,
		IRequestClient<SelectCourseInfoEvent> requestCourseSelectEvent) : ICartService
	{
		public async Task<AddToCartResponse> AddToCartAsync(Guid courseId, CancellationToken ct = default)
		{
			var response = new AddToCartResponse { Success = false };

			var currentUser = identityService.GetCurrentUser()!;
			
			var cart = await cartRepository
				.Find(x => x.UserId == currentUser.UserId &&
						   x.IsActive,
					isTracking: true, ct)
				.FirstOrDefaultAsync(ct);

			if (cart is null)
			{
				cart = new Cart
				{
					UserId = currentUser.UserId,
				};

				await cartRepository.AddAsync(cart);
				await unitOfWork.SaveChangesAsync(currentUser.Email, ct);
			}

			var existingItem = await cartItemRepository
				.Find(x => x.CartId == cart.CartId &&
						   x.CourseId == courseId && 
						   x.IsActive
					,isTracking: false, ct)
				.FirstOrDefaultAsync(ct);

			if (existingItem is not null)
			{
				response.SetMessage(MessageId.I00000, "Khóa học đã có trong giỏ hàng");
				return response;
			}
			
			// Publish event to CourseService to get course details
			var couseSelectEvent = await requestCourseSelectEvent
				.GetResponse<SelectCourseInfoEventResponse>(
					new SelectCourseInfoEvent{ CourseId = courseId }, ct);
			if (!couseSelectEvent.Message.Success)
			{
				response.SetMessage(MessageId.I00000, couseSelectEvent.Message.Message);
				return response;
			}
			
			var courseInfo = couseSelectEvent.Message.Response;

			var cartItem = new CartItem
			{
				CartId = cart.CartId,
				CourseId = courseId,
				CourseTitleSnapshot = courseInfo.Title,
				CourseImageUrlSnapshot = courseInfo.ImageUrl,
				PriceSnapshot = courseInfo.Price,
				DealPriceSnapshot = courseInfo.DealPrice,
				IsSelected = true,
			};

			await cartItemRepository.AddAsync(cartItem);
			await unitOfWork.SaveChangesAsync(currentUser.Email, ct);

			response.Success = true;
			response.SetMessage(MessageId.I00001, "Thêm khóa học vào giỏ hàng");
			return response;
		}

		public async Task<CheckCourseInCartResponse> CheckCourseInMyCartAsync(Guid courseId, CancellationToken ct = default)
		{
			var response = new CheckCourseInCartResponse { Success = false };

			var currentUser = identityService.GetCurrentUser();
			if (currentUser is null)
			{
				response.SetMessage(MessageId.E00000, "Người dùng chưa đăng nhập");
				return response;
			}

			var cart = await cartRepository
				.Find(x => x.UserId == currentUser.UserId &&
						   x.IsActive,
					isTracking: false, ct)
				.FirstOrDefaultAsync(ct);

			if (cart is null)
			{
				response.Success = true;
				response.Response = new CheckCourseInCartDto
				{
					IsInCart = false,
					CartId = null,
					CartItemId = null
				};
				response.SetMessage(MessageId.I00001, "Giỏ hàng trống");
				return response;
			}

			var cartItem = await cartItemRepository
				.Find(x => x.CartId == cart.CartId &&
						   x.CourseId == courseId &&
						   x.IsActive,
					isTracking: false, ct)
				.FirstOrDefaultAsync(ct);

			var isInCart = cartItem is not null;

			response.Success = true;
			response.Response = new CheckCourseInCartDto
			{
				IsInCart = isInCart,
				CartId = isInCart ? cart.CartId : null,
				CartItemId = isInCart ? cartItem!.CartItemId : null
			};

			response.SetMessage(MessageId.I00001, "Kiểm tra khóa học trong giỏ hàng");
			return response;
		}

		public async Task<GetMyCartResponse> GetMyCartAsync(CancellationToken ct = default)
		{
			var response = new GetMyCartResponse { Success = false };

			var currentUser = identityService.GetCurrentUser();
			if (currentUser is null)
			{
				response.SetMessage(MessageId.E00000, "Người dùng chưa đăng nhập");
				return response;
			}

			// Lấy cart Active
			var cart = await cartRepository
				.Find(x => x.UserId == currentUser.UserId &&
						   x.IsActive,
					isTracking: false, ct)
				.FirstOrDefaultAsync(ct);

			if (cart is null)
			{
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

			var items = await cartItemRepository
				.Find(x => x.CartId == cart.CartId &&
						   x.IsActive,
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

			var currentUser = identityService.GetCurrentUser();
			if (currentUser is null)
			{
				response.SetMessage(MessageId.E00000, "Người dùng chưa đăng nhập");
				return response;
			}

			var cart = await cartRepository
				.Find(x => x.UserId == currentUser.UserId &&
						   x.IsActive,
					isTracking: false, ct)
				.FirstOrDefaultAsync(ct);

			if (cart is null)
			{
				response.SetMessage(MessageId.E00000, "Không tìm thấy giỏ hàng");
				return response;
			}

			var item = await cartItemRepository
				.Find(x => x.CartItemId == cartItemId &&
						   x.CartId == cart.CartId &&
						   x.IsActive,
					isTracking: true, ct)
				.FirstOrDefaultAsync(ct);

			if (item is null)
			{
				response.SetMessage(MessageId.E00000, "Không tìm thấy item trong giỏ hàng");
				return response;
			}
			cartItemRepository.Update(item);
			await unitOfWork.SaveChangesAsync(currentUser.Email, ct, needLogicalDelete: true);

			response.Success = true;
			response.SetMessage(MessageId.I00001, "Xóa item khỏi giỏ hàng");
			return response;
		}

		public async Task<UpdateCartItemResponse> UpdateCartItemAsync(Guid cartItemId, bool? isSelected, CancellationToken ct = default)
		{
			var response = new UpdateCartItemResponse { Success = false };

			var currentUser = identityService.GetCurrentUser();
			if (currentUser is null)
			{
				response.SetMessage(MessageId.E00000, "Người dùng chưa đăng nhập");
				return response;
			}

			// Lấy cart active của user
			var cart = await cartRepository
				.Find(x => x.UserId == currentUser.UserId &&
						   x.IsActive,
					isTracking: false, ct)
				.FirstOrDefaultAsync(ct);

			if (cart is null)
			{
				response.SetMessage(MessageId.E00000, "Không tìm thấy giỏ hàng");
				return response;
			}

			var item = await cartItemRepository
				.Find(x => x.CartItemId == cartItemId &&
						   x.CartId == cart.CartId &&
						   x.IsActive,
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

			cartItemRepository.Update(item);
			await unitOfWork.SaveChangesAsync(ct);

			response.Success = true;
			response.SetMessage(MessageId.I00001, "Cập nhật giỏ hàng");
			return response;
		}
	}
}
