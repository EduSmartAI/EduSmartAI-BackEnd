using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using Course.Application.Majors.Commands.UpdateMajorDescription;
using Course.Domain.Models;
using Course.Infrastructure.Implements;
using Moq;
using NUnit.Framework;

namespace Services.Tests.CourseServiceTests;

[TestFixture]
public class UpdateMajorTests
{
    private Mock<IIdentityService> _mockIdentityService = null!;
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<ICommandRepository<Major>> _mockMajorRepository = null!;
    private MajorService _majorService = null!;

    private readonly Guid _majorId = Guid.NewGuid();
    private readonly string _newDescription = "Updated description for major";

    [SetUp]
    public void Setup()
    {
        _mockIdentityService = new Mock<IIdentityService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMajorRepository = new Mock<ICommandRepository<Major>>();

        _majorService = new MajorService(
            _mockIdentityService.Object,
            _mockUnitOfWork.Object,
            _mockMajorRepository.Object
        );
    }

    [Test]
    public async Task UpdateMajor_UserNotAuthenticated_ShouldReturnFailed()
    {
        // Arrange
        _mockIdentityService.Setup(x => x.GetCurrentUser()).Returns((BaseService.Application.Interfaces.IdentityHepers.IdentityEntity?)null);

        // Act
        var result = await _majorService.UpdateMajorDescriptionAsync(_majorId, _newDescription, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Contains.Substring("không đăng nhập"));
    }

    [Test]
    public async Task UpdateMajor_MajorNotFound_ShouldReturnFailed()
    {
        // Arrange
        var user = new BaseService.Application.Interfaces.IdentityHepers.IdentityEntity
        {
            UserId = Guid.NewGuid(),
            Email = "test@test.com",
            FullName = "Test User",
            RoleName = "Admin"
        };

        _mockIdentityService.Setup(x => x.GetCurrentUser()).Returns(user);
        _mockMajorRepository.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<Major, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<System.Linq.Expressions.Expression<System.Func<Major, object>>[]>()))
            .ReturnsAsync((Major?)null);

        // Act
        var result = await _majorService.UpdateMajorDescriptionAsync(_majorId, _newDescription, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Contains.Substring("Không tìm thấy ngành học"));
    }

    [Test]
    public async Task UpdateMajor_ValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var user = new BaseService.Application.Interfaces.IdentityHepers.IdentityEntity
        {
            UserId = Guid.NewGuid(),
            Email = "test@test.com",
            FullName = "Test User",
            RoleName = "Admin"
        };

        var existingMajor = new Major
        {
            MajorId = _majorId,
            MajorCode = "SE",
            MajorName = "Software Engineering",
            Description = "Old description"
        };

        _mockIdentityService.Setup(x => x.GetCurrentUser()).Returns(user);
        _mockMajorRepository.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<Major, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<System.Linq.Expressions.Expression<System.Func<Major, object>>[]>()))
            .ReturnsAsync(existingMajor);

        _mockMajorRepository.Setup(x => x.Update(
            It.IsAny<Major>(),
            It.IsAny<string>(),
            It.IsAny<bool>()))
            .Verifiable();

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<bool>()))
            .ReturnsAsync(1);

        // Act
        var result = await _majorService.UpdateMajorDescriptionAsync(_majorId, _newDescription, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Response, Is.True);
        Assert.That(result.Message, Contains.Substring("Cập nhật mô tả ngành học thành công"));

        _mockMajorRepository.Verify(x => x.Update(
            It.Is<Major>(m => m.MajorId == _majorId && m.Description == _newDescription.Trim()),
            user.Email,
            It.IsAny<bool>()), Times.Once);
    }

    [Test]
    public async Task UpdateMajor_WithNullDescription_ShouldReturnSuccess()
    {
        // Arrange
        var user = new BaseService.Application.Interfaces.IdentityHepers.IdentityEntity
        {
            UserId = Guid.NewGuid(),
            Email = "test@test.com",
            FullName = "Test User",
            RoleName = "Admin"
        };

        var existingMajor = new Major
        {
            MajorId = _majorId,
            MajorCode = "SE",
            MajorName = "Software Engineering",
            Description = "Old description"
        };

        _mockIdentityService.Setup(x => x.GetCurrentUser()).Returns(user);
        _mockMajorRepository.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<Major, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<System.Linq.Expressions.Expression<System.Func<Major, object>>[]>()))
            .ReturnsAsync(existingMajor);

        _mockMajorRepository.Setup(x => x.Update(
            It.IsAny<Major>(),
            It.IsAny<string>(),
            It.IsAny<bool>()))
            .Verifiable();

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<bool>()))
            .ReturnsAsync(1);

        // Act
        var result = await _majorService.UpdateMajorDescriptionAsync(_majorId, null, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Response, Is.True);

        _mockMajorRepository.Verify(x => x.Update(
            It.Is<Major>(m => m.MajorId == _majorId && m.Description == null),
            user.Email,
            It.IsAny<bool>()), Times.Once);
    }
}

