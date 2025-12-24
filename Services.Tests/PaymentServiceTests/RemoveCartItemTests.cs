using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using PaymentService.Application.Applications.Carts.Commands.RemoveCart;
using PaymentService.Domain.WriteModels;
using PaymentService.Infrastructure.Implements;
using Services.Tests.Helpers;

namespace Services.Tests.PaymentServiceTests;

[TestFixture]
public class RemoveCartItemTests
{
    private Mock<IIdentityService> _mockIdentityService = null!;
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<ICommandRepository<Cart>> _mockCartRepository = null!;
    private Mock<ICommandRepository<CartItem>> _mockCartItemRepository = null!;
    private Mock<MassTransit.IRequestClient<BuildingBlocks.Messaging.Events.CourseService.SelectCourseInfoEvent>> _mockCourseSelectClient = null!;
    private Mock<MassTransit.IRequestClient<BuildingBlocks.Messaging.Events.PaymentService.CheckIsCourseEnrolledEvent>> _mockCheckEnrollClient = null!;
    private CartService _cartService = null!;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _cartId = Guid.NewGuid();
    private readonly Guid _cartItemId = Guid.NewGuid();

    [SetUp]
    public void Setup()
    {
        _mockIdentityService = new Mock<IIdentityService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCartRepository = new Mock<ICommandRepository<Cart>>();
        _mockCartItemRepository = new Mock<ICommandRepository<CartItem>>();
        _mockCourseSelectClient = new Mock<MassTransit.IRequestClient<BuildingBlocks.Messaging.Events.CourseService.SelectCourseInfoEvent>>();
        _mockCheckEnrollClient = new Mock<MassTransit.IRequestClient<BuildingBlocks.Messaging.Events.PaymentService.CheckIsCourseEnrolledEvent>>();

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
    public async Task RemoveCartItem_UserNotAuthenticated_ShouldReturnFailed()
    {
        // Arrange
        _mockIdentityService.Setup(x => x.GetCurrentUser()).Returns((BaseService.Application.Interfaces.IdentityHepers.IdentityEntity?)null);

        // Act
        var result = await _cartService.RemoveCartItemAsync(_cartItemId, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Contains.Substring("chưa đăng nhập"));
    }

    [Test]
    public async Task RemoveCartItem_CartNotFound_ShouldReturnFailed()
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

        var emptyCarts = Enumerable.Empty<Cart>().AsQueryable();
        var asyncEmptyCarts = new AsyncEnumerable<Cart>(emptyCarts.Expression);

        _mockCartRepository.Setup(x => x.Find(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<Cart, bool>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Func<System.Linq.IQueryable<Cart>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Cart, object>>>()))
            .Returns(asyncEmptyCarts);

        // Act
        var result = await _cartService.RemoveCartItemAsync(_cartItemId, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Contains.Substring("Không tìm thấy giỏ hàng"));
    }

    [Test]
    public async Task RemoveCartItem_ItemNotFound_ShouldReturnFailed()
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

        // Act
        var result = await _cartService.RemoveCartItemAsync(_cartItemId, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Contains.Substring("Không tìm thấy item"));
    }

    [Test]
    public async Task RemoveCartItem_ValidRequest_ShouldReturnSuccess()
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

        var cartItem = new CartItem
        {
            CartItemId = _cartItemId,
            CartId = _cartId,
            CourseId = Guid.NewGuid(),
            IsActive = true
        };

        _mockIdentityService.Setup(x => x.GetCurrentUser()).Returns(user);

        var carts = new[] { cart }.AsQueryable();
        var asyncCarts = new AsyncEnumerable<Cart>(carts.Expression);

        _mockCartRepository.Setup(x => x.Find(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<Cart, bool>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Func<System.Linq.IQueryable<Cart>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Cart, object>>>()))
            .Returns(asyncCarts);

        var items = new[] { cartItem }.AsQueryable();
        var asyncItems = new AsyncEnumerable<CartItem>(items.Expression);

        _mockCartItemRepository.Setup(x => x.Find(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<CartItem, bool>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Func<System.Linq.IQueryable<CartItem>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CartItem, object>>>()))
            .Returns(asyncItems);

        _mockCartItemRepository.Setup(x => x.Update(
            It.IsAny<CartItem>()))
            .Verifiable();

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<bool>()))
            .ReturnsAsync(1);

        // Act
        var result = await _cartService.RemoveCartItemAsync(_cartItemId, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Message, Contains.Substring("Xóa item khỏi giỏ hàng"));

        _mockCartItemRepository.Verify(x => x.Update(
            It.Is<CartItem>(ci => ci.CartItemId == _cartItemId)), Times.Once);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(
            user.Email,
            It.IsAny<CancellationToken>(),
            true), Times.Once);
    }
}

