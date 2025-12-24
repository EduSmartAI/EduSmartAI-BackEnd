using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Infrastructure.Contexts;
using Course.Application.Syllabus.Commands.CreateSyllabus;
using Course.Domain.Models;
using Course.Infrastructure.Implements;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace Services.Tests.CourseServiceTests;

[TestFixture]
public class CreateSyllabusTests
{
    private Mock<IIdentityService> _mockIdentityService = null!;
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<ICommandRepository<Syllabus>> _mockSyllabusRepository = null!;
    private Mock<ICommandRepository<SyllabusSemester>> _mockSyllabusSemesterRepository = null!;
    private Mock<ICommandRepository<SyllabusSubject>> _mockSyllabusSubjectRepository = null!;
    private Mock<ICommandRepository<Semester>> _mockSemesterRepository = null!;
    private Mock<ICommandRepository<Subject>> _mockSubjectRepository = null!;
    private SyllabusService _syllabusService = null!;

    private readonly Guid _majorId = Guid.NewGuid();
    private readonly string _versionLabel = "2024.1";

    [SetUp]
    public void Setup()
    {
        _mockIdentityService = new Mock<IIdentityService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockSyllabusRepository = new Mock<ICommandRepository<Syllabus>>();
        _mockSyllabusSemesterRepository = new Mock<ICommandRepository<SyllabusSemester>>();
        _mockSyllabusSubjectRepository = new Mock<ICommandRepository<SyllabusSubject>>();
        _mockSemesterRepository = new Mock<ICommandRepository<Semester>>();
        _mockSubjectRepository = new Mock<ICommandRepository<Subject>>();

        // Create in-memory database for AppDbContext
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var testDbContext = new TestAppDbContext(options);
        
        _syllabusService = new SyllabusService(
            _mockIdentityService.Object,
            _mockUnitOfWork.Object,
            testDbContext,
            _mockSyllabusRepository.Object,
            _mockSyllabusSemesterRepository.Object,
            _mockSyllabusSubjectRepository.Object,
            _mockSemesterRepository.Object,
            _mockSubjectRepository.Object
        );
    }

    // Test implementation of AppDbContext
    private class TestAppDbContext : AppDbContext
    {
        public TestAppDbContext(DbContextOptions options) : base(options)
        {
        }
    }

    [Test]
    public async Task CreateSyllabus_VersionLabelAlreadyExists_ShouldReturnFailed()
    {
        // Arrange
        var user = new BaseService.Application.Interfaces.IdentityHepers.IdentityEntity
        {
            UserId = Guid.NewGuid(),
            Email = "test@test.com",
            FullName = "Test User",
            RoleName = "Admin"
        };

        var existingSyllabus = new Syllabus
        {
            SyllabusId = Guid.NewGuid(),
            MajorId = _majorId,
            VersionLabel = _versionLabel
        };

        var request = new CreateSyllabusCommand(
            _majorId,
            _versionLabel,
            DateOnly.FromDateTime(DateTime.UtcNow),
            null
        );

        _mockIdentityService.Setup(x => x.GetCurrentUser()).Returns(user);
        _mockSyllabusRepository.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<Syllabus, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<System.Linq.Expressions.Expression<System.Func<Syllabus, object>>[]>()))
            .ReturnsAsync(existingSyllabus);

        // Act
        var result = await _syllabusService.CreateSyllabusAsync(request, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Contains.Substring("đã tồn tại"));
    }

    [Test]
    public async Task CreateSyllabus_ValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var user = new BaseService.Application.Interfaces.IdentityHepers.IdentityEntity
        {
            UserId = Guid.NewGuid(),
            Email = "test@test.com",
            FullName = "Test User",
            RoleName = "Admin"
        };

        var request = new CreateSyllabusCommand(
            _majorId,
            _versionLabel,
            DateOnly.FromDateTime(DateTime.UtcNow),
            null
        );

        _mockIdentityService.Setup(x => x.GetCurrentUser()).Returns(user);
        _mockSyllabusRepository.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<Syllabus, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<System.Linq.Expressions.Expression<System.Func<Syllabus, object>>[]>()))
            .ReturnsAsync((Syllabus?)null);

        _mockSyllabusRepository.Setup(x => x.AddAsync(
            It.IsAny<Syllabus>(),
            It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>(),
            false))
            .ReturnsAsync(1);

        // Act
        var result = await _syllabusService.CreateSyllabusAsync(request, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Response, Is.True);
        Assert.That(result.Message, Contains.Substring("Tạo tên khóa"));

        _mockSyllabusRepository.Verify(x => x.AddAsync(
            It.Is<Syllabus>(s => s.MajorId == _majorId && s.VersionLabel == _versionLabel),
            user.Email), Times.Once);
    }
}

