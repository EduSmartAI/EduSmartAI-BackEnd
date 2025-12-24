using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BuildingBlocks.Messaging.Events.CourseService;
using BuildingBlocks.Messaging.Events.PaymentService;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using PaymentService.Application.Applications.Carts.Commands.AddToCart;
using PaymentService.Domain.WriteModels;
using PaymentService.Infrastructure.Implements;
using Services.Tests.Helpers;

namespace Services.Tests.PaymentServiceTests;

[TestFixture]
public class AddToCartTests
{
    private Mock<IIdentityService> _mockIdentityService = null!;
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<ICommandRepository<Cart>> _mockCartRepository = null!;
    private Mock<ICommandRepository<CartItem>> _mockCartItemRepository = null!;
    private Mock<IRequestClient<SelectCourseInfoEvent>> _mockCourseSelectClient = null!;
    private Mock<IRequestClient<CheckIsCourseEnrolledEvent>> _mockCheckEnrollClient = null!;
    private CartService _cartService = null!;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _courseId = Guid.NewGuid();
    private readonly Guid _cartId = Guid.NewGuid();

    [SetUp]
    public void Setup()
    {
        _mockIdentityService = new Mock<IIdentityService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCartRepository = new Mock<ICommandRepository<Cart>>();
        _mockCartItemRepository = new Mock<ICommandRepository<CartItem>>();
        _mockCourseSelectClient = new Mock<IRequestClient<SelectCourseInfoEvent>>();
        _mockCheckEnrollClient = new Mock<IRequestClient<CheckIsCourseEnrolledEvent>>();

        _cartService = new CartService(
            _mockIdentityService.Object,
            _mockUnitOfWork.Object,
            _mockCartRepository.Object,
            _mockCartItemRepository.Object,
            _mockCourseSelectClient.Object,
            _mockCheckEnrollClient.Object
        );
    }

    [Test]
    public async Task AddToCart_UserAlreadyEnrolled_ShouldReturnFailed()
    {
        // Arrange
        var user = new BaseService.Application.Interfaces.IdentityHepers.IdentityEntity
        {
            UserId = _userId,
            Email = "test@test.com",
            FullName = "Test User",
            RoleName = "Student"
        };

        _mockIdentityService.Setup(x => x.GetCurrentUser()).Returns(user);

        var enrollResponseMessage = new CheckIsCourseEnrolledEventResponse { Success = true, Response = true };
        var mockResponse = new TestResponse<CheckIsCourseEnrolledEventResponse>(enrollResponseMessage);
        
        _mockCheckEnrollClient.Setup(x => x.GetResponse<CheckIsCourseEnrolledEventResponse>(
            It.IsAny<object>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<RequestTimeout>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _cartService.AddToCartAsync(_courseId, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Contains.Substring("đã đăng ký khóa học"));
    }

    [Test]
    public async Task AddToCart_CourseAlreadyInCart_ShouldReturnFailed()
    {
        // Arrange
        var user = new BaseService.Application.Interfaces.IdentityHepers.IdentityEntity
        {
            UserId = _userId,
            Email = "test@test.com",
            FullName = "Test User",
            RoleName = "Student"
        };

        var cart = new Cart
        {
            CartId = _cartId,
            UserId = _userId,
            IsActive = true
        };

        var existingItem = new CartItem
        {
            CartItemId = Guid.NewGuid(),
            CartId = _cartId,
            CourseId = _courseId,
            IsActive = true
        };

        _mockIdentityService.Setup(x => x.GetCurrentUser()).Returns(user);

        var enrollResponseMessage = new CheckIsCourseEnrolledEventResponse { Success = true, Response = false };
        var mockResponse = new Mock<Response<CheckIsCourseEnrolledEventResponse>>();
        mockResponse.Setup(r => r.Message).Returns(enrollResponseMessage);
        
        _mockCheckEnrollClient.Setup(x => x.GetResponse<CheckIsCourseEnrolledEventResponse>(
            It.IsAny<object>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<RequestTimeout>()))
            .ReturnsAsync(mockResponse.Object);

        var carts = new[] { cart }.AsQueryable();
        var asyncCarts = new AsyncEnumerable<Cart>(carts.Expression);

        _mockCartRepository.Setup(x => x.Find(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<Cart, bool>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Func<System.Linq.IQueryable<Cart>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Cart, object>>>()))
            .Returns(asyncCarts);

        var items = new[] { existingItem }.AsQueryable();
        var asyncItems = new AsyncEnumerable<CartItem>(items.Expression);

        _mockCartItemRepository.Setup(x => x.Find(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<CartItem, bool>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Func<System.Linq.IQueryable<CartItem>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CartItem, object>>>()))
            .Returns(asyncItems);

        // Act
        var result = await _cartService.AddToCartAsync(_courseId, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Contains.Substring("đã có trong giỏ hàng"));
    }

    [Test]
    public async Task AddToCart_ValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var user = new BaseService.Application.Interfaces.IdentityHepers.IdentityEntity
        {
            UserId = _userId,
            Email = "test@test.com",
            FullName = "Test User",
            RoleName = "Student"
        };

        var cart = new Cart
        {
            CartId = _cartId,
            UserId = _userId,
            IsActive = true
        };

        _mockIdentityService.Setup(x => x.GetCurrentUser()).Returns(user);

        var enrollResponseMessage = new CheckIsCourseEnrolledEventResponse { Success = true, Response = false };
        var mockResponse = new Mock<Response<CheckIsCourseEnrolledEventResponse>>();
        mockResponse.Setup(r => r.Message).Returns(enrollResponseMessage);
        
        _mockCheckEnrollClient.Setup(x => x.GetResponse<CheckIsCourseEnrolledEventResponse>(
            It.IsAny<object>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<RequestTimeout>()))
            .ReturnsAsync(mockResponse.Object);

        var carts = new[] { cart }.AsQueryable();
        var asyncCarts = new AsyncEnumerable<Cart>(carts.Expression);

        _mockCartRepository.Setup(x => x.Find(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<Cart, bool>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Func<System.Linq.IQueryable<Cart>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Cart, object>>>()))
            .Returns(asyncCarts);

        var emptyItems = Enumerable.Empty<CartItem>().AsQueryable();
        var asyncEmptyItems = new AsyncEnumerable<CartItem>(emptyItems.Expression);

        _mockCartItemRepository.Setup(x => x.Find(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<CartItem, bool>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Func<System.Linq.IQueryable<CartItem>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CartItem, object>>>()))
            .Returns(asyncEmptyItems);

        var courseInfo = new SelectCourseInfoEventResponse
        {
            Success = true,
            Response = new SelectCourseInfoEventResponseEntity
            {
                CourseId = _courseId,
                Title = "Test Course",
                ImageUrl = "https://example.com/image.jpg",
                Price = 100000,
                DealPrice = 80000
            }
        };

        var mockCourseResponse = new TestResponse<SelectCourseInfoEventResponse>(courseInfo);
        
        _mockCourseSelectClient.Setup(x => x.GetResponse<SelectCourseInfoEventResponse>(
            It.IsAny<object>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<RequestTimeout>()))
            .ReturnsAsync(mockCourseResponse);

        _mockCartItemRepository.Setup(x => x.AddAsync(
            It.IsAny<CartItem>(),
            It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<bool>()))
            .ReturnsAsync(1);

        // Act
        var result = await _cartService.AddToCartAsync(_courseId, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Message, Contains.Substring("Thêm khóa học vào giỏ hàng"));

        _mockCartItemRepository.Verify(x => x.AddAsync(
            It.Is<CartItem>(ci => ci.CartId == _cartId && ci.CourseId == _courseId),
            user.Email), Times.Once);
    }

    [Test]
    public async Task AddToCart_CartNotExists_ShouldCreateCart()
    {
        // Arrange
        var user = new BaseService.Application.Interfaces.IdentityHepers.IdentityEntity
        {
            UserId = _userId,
            Email = "test@test.com",
            FullName = "Test User",
            RoleName = "Student"
        };

        _mockIdentityService.Setup(x => x.GetCurrentUser()).Returns(user);

        var enrollResponseMessage = new CheckIsCourseEnrolledEventResponse { Success = true, Response = false };
        var mockResponse = new Mock<Response<CheckIsCourseEnrolledEventResponse>>();
        mockResponse.Setup(r => r.Message).Returns(enrollResponseMessage);
        
        _mockCheckEnrollClient.Setup(x => x.GetResponse<CheckIsCourseEnrolledEventResponse>(
            It.IsAny<object>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<RequestTimeout>()))
            .ReturnsAsync(mockResponse.Object);

        var emptyCarts = Enumerable.Empty<Cart>().AsQueryable();
        var asyncEmptyCarts = new AsyncEnumerable<Cart>(emptyCarts.Expression);

        _mockCartRepository.Setup(x => x.Find(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<Cart, bool>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Func<System.Linq.IQueryable<Cart>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Cart, object>>>()))
            .Returns(asyncEmptyCarts);

        _mockCartRepository.Setup(x => x.AddAsync(
            It.IsAny<Cart>(),
            It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var emptyItems = Enumerable.Empty<CartItem>().AsQueryable();
        var asyncEmptyItems = new AsyncEnumerable<CartItem>(emptyItems.Expression);

        _mockCartItemRepository.Setup(x => x.Find(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<CartItem, bool>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Func<System.Linq.IQueryable<CartItem>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CartItem, object>>>()))
            .Returns(asyncEmptyItems);

        var courseInfo = new SelectCourseInfoEventResponse
        {
            Success = true,
            Response = new SelectCourseInfoEventResponseEntity
            {
                CourseId = _courseId,
                Title = "Test Course",
                ImageUrl = "https://example.com/image.jpg",
                Price = 100000,
                DealPrice = 80000
            }
        };

        var mockCourseResponse = new TestResponse<SelectCourseInfoEventResponse>(courseInfo);
        
        _mockCourseSelectClient.Setup(x => x.GetResponse<SelectCourseInfoEventResponse>(
            It.IsAny<object>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<RequestTimeout>()))
            .ReturnsAsync(mockCourseResponse);

        _mockCartItemRepository.Setup(x => x.AddAsync(
            It.IsAny<CartItem>(),
            It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<bool>()))
            .ReturnsAsync(1);

        // Act
        var result = await _cartService.AddToCartAsync(_courseId, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.True);
        _mockCartRepository.Verify(x => x.AddAsync(
            It.Is<Cart>(c => c.UserId == _userId),
            user.Email), Times.Once);
    }
}

