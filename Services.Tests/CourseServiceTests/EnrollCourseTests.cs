using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using Course.Application.UserLessonProgresses.Commands.EnrollCourse;
using Course.Domain.Models;
using Course.Domain.ReadModels;
using Course.Infrastructure.Implements;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Services.Tests.Helpers;
using StackExchange.Redis;
using CourseEntity = Course.Domain.Models.Course;

namespace Services.Tests.CourseServiceTests;

[TestFixture]
public class EnrollCourseTests
{
    private Mock<IDatabase> _mockCache = null!;
    private Mock<IIdentityService> _mockIdentityService = null!;
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<ICommandRepository<CourseEntity>> _mockCourseRepository = null!;
    private Mock<ICommandRepository<Lesson>> _mockLessonRepository = null!;
    private Mock<ICommandRepository<Module>> _mockModuleRepository = null!;
    private Mock<ICommandRepository<CourseStudentEnrollment>> _mockEnrollmentRepository = null!;
    private Mock<IQueryRepository<CourseStudentEnrollmentCollection>> _mockEnrollmentQueryRepository = null!;
    private Mock<ICommandRepository<ModuleQuiz>> _mockModuleQuizRepository = null!;
    private Mock<ICommandRepository<LessonQuiz>> _mockLessonQuizRepository = null!;
    private Mock<ICommandRepository<UserLessonProgress>> _mockUserLessonProgressRepository = null!;
    private Mock<ICommandRepository<UserModuleProgress>> _mockUserModuleProgressRepository = null!;
    private Mock<ICommandRepository<UserCourseProgress>> _mockUserCourseProgressRepository = null!;
    private Mock<ICommandRepository<CourseRating>> _mockRatingRepository = null!;
    private Mock<IPublishEndpoint> _mockPublishEndpoint = null!;
    private Mock<Course.Application.Interfaces.Helpers.Courses.ICourseCache> _mockCourseCache = null!;
    private Mock<Course.Application.Interfaces.Helpers.Courses.ICourseMapper> _mockCourseMapper = null!;
    private Mock<Course.Application.Interfaces.Helpers.Courses.IQuizGateway> _mockQuizGateway = null!;
    private StudentProgressService _studentProgressService = null!;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _courseId = Guid.NewGuid();

    [SetUp]
    public void Setup()
    {
        _mockCache = new Mock<IDatabase>();
        _mockIdentityService = new Mock<IIdentityService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCourseRepository = new Mock<ICommandRepository<CourseEntity>>();
        _mockLessonRepository = new Mock<ICommandRepository<Lesson>>();
        _mockModuleRepository = new Mock<ICommandRepository<Module>>();
        _mockEnrollmentRepository = new Mock<ICommandRepository<CourseStudentEnrollment>>();
        _mockEnrollmentQueryRepository = new Mock<IQueryRepository<CourseStudentEnrollmentCollection>>();
        _mockModuleQuizRepository = new Mock<ICommandRepository<ModuleQuiz>>();
        _mockLessonQuizRepository = new Mock<ICommandRepository<LessonQuiz>>();
        _mockUserLessonProgressRepository = new Mock<ICommandRepository<UserLessonProgress>>();
        _mockUserModuleProgressRepository = new Mock<ICommandRepository<UserModuleProgress>>();
        _mockUserCourseProgressRepository = new Mock<ICommandRepository<UserCourseProgress>>();
        _mockRatingRepository = new Mock<ICommandRepository<CourseRating>>();
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        _mockCourseCache = new Mock<Course.Application.Interfaces.Helpers.Courses.ICourseCache>();
        _mockCourseMapper = new Mock<Course.Application.Interfaces.Helpers.Courses.ICourseMapper>();
        _mockQuizGateway = new Mock<Course.Application.Interfaces.Helpers.Courses.IQuizGateway>();

        _studentProgressService = new StudentProgressService(
            _mockCache.Object,
            _mockIdentityService.Object,
            _mockUnitOfWork.Object,
            _mockCourseRepository.Object,
            _mockLessonRepository.Object,
            _mockModuleRepository.Object,
            _mockEnrollmentRepository.Object,
            _mockEnrollmentQueryRepository.Object,
            _mockModuleQuizRepository.Object,
            _mockLessonQuizRepository.Object,
            _mockUserLessonProgressRepository.Object,
            _mockUserModuleProgressRepository.Object,
            _mockUserCourseProgressRepository.Object,
            _mockRatingRepository.Object,
            _mockPublishEndpoint.Object,
            _mockCourseCache.Object,
            _mockCourseMapper.Object,
            _mockQuizGateway.Object
        );
    }

    [Test]
    public async Task EnrollCourse_UserNotAuthenticated_ShouldReturnFailed()
    {
        // Arrange
        _mockIdentityService.Setup(x => x.GetCurrentUser()).Returns((IdentityEntity?)null);

        // Act
        var result = await _studentProgressService.EnrollCourseAsync(_courseId);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Contains.Substring("chưa đăng nhập"));
    }

    [Test]
    public async Task EnrollCourse_UserAlreadyEnrolled_ShouldReturnFailed()
    {
        // Arrange
        var user = new BaseService.Application.Interfaces.IdentityHepers.IdentityEntity
        {
            UserId = _userId,
            Email = "test@test.com",
            FullName = "Test User",
            RoleName = "Student"
        };

        var existingEnrollment = new CourseStudentEnrollment
        {
            EnrollmentId = Guid.NewGuid(),
            CourseId = _courseId,
            UserId = _userId,
            IsActive = true
        };

        _mockIdentityService.Setup(x => x.GetCurrentUser()).Returns(user);
        
        // Create async queryable for Find().FirstOrDefaultAsync()
        var enrollments = new[] { existingEnrollment }.AsQueryable();
        var asyncEnrollments = new AsyncEnumerable<CourseStudentEnrollment>(enrollments.Expression);
        
        _mockEnrollmentRepository.Setup(x => x.Find(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<CourseStudentEnrollment, bool>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Func<System.Linq.IQueryable<CourseStudentEnrollment>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CourseStudentEnrollment, object>>>()))
            .Returns(asyncEnrollments);

        // Act
        var result = await _studentProgressService.EnrollCourseAsync(_courseId);

        // Assert
        Assert.That(result.Success, Is.False);
        // MessageId.E11004 maps to "Thông tin người dùng đã được đăng ký. Vui lòng nhập thông tin khác"
        Assert.That(result.MessageId, Is.EqualTo("E11004"));
    }

    [Test]
    public async Task EnrollCourse_CourseNotFound_ShouldReturnFailed()
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
        
        // Create async queryable for Find().FirstOrDefaultAsync()
        var emptyEnrollments = Enumerable.Empty<CourseStudentEnrollment>().AsQueryable();
        var asyncEmptyEnrollments = new AsyncEnumerable<CourseStudentEnrollment>(emptyEnrollments.Expression);
        
        var emptyCourses = Enumerable.Empty<CourseEntity>().AsQueryable();
        var asyncEmptyCourses = new AsyncEnumerable<CourseEntity>(emptyCourses.Expression);
        
        _mockEnrollmentRepository.Setup(x => x.Find(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<CourseStudentEnrollment, bool>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Func<System.Linq.IQueryable<CourseStudentEnrollment>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CourseStudentEnrollment, object>>>()))
            .Returns(asyncEmptyEnrollments);

        _mockCourseRepository.Setup(x => x.Find(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<CourseEntity, bool>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Func<System.Linq.IQueryable<CourseEntity>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CourseEntity, object>>>()))
            .Returns(asyncEmptyCourses);

        // Act
        var result = await _studentProgressService.EnrollCourseAsync(_courseId);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Contains.Substring("Không tìm thấy khóa học"));
    }

    [Test]
    public async Task EnrollCourse_ValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var user = new BaseService.Application.Interfaces.IdentityHepers.IdentityEntity
        {
            UserId = _userId,
            Email = "test@test.com",
            FullName = "Test User",
            RoleName = "Student"
        };

        var course = new CourseEntity
        {
            CourseId = _courseId,
            IsActive = true
        };

        _mockIdentityService.Setup(x => x.GetCurrentUser()).Returns(user);
        
        // Create async queryable for Find().FirstOrDefaultAsync()
        var emptyEnrollments = Enumerable.Empty<CourseStudentEnrollment>().AsQueryable();
        var asyncEmptyEnrollments = new AsyncEnumerable<CourseStudentEnrollment>(emptyEnrollments.Expression);
        
        var courses = new[] { course }.AsQueryable();
        var asyncCourses = new AsyncEnumerable<CourseEntity>(courses.Expression);
        
        _mockEnrollmentRepository.Setup(x => x.Find(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<CourseStudentEnrollment, bool>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Func<System.Linq.IQueryable<CourseStudentEnrollment>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CourseStudentEnrollment, object>>>()))
            .Returns(asyncEmptyEnrollments);

        _mockCourseRepository.Setup(x => x.Find(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<CourseEntity, bool>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Func<System.Linq.IQueryable<CourseEntity>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CourseEntity, object>>>()))
            .Returns(asyncCourses);

        _mockUnitOfWork.Setup(x => x.SessionSaveChangesAsync())
            .Returns(Task.CompletedTask);

        _mockEnrollmentRepository.Setup(x => x.AddAsync(
            It.IsAny<CourseStudentEnrollment>(),
            It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(
            It.IsAny<Func<Task<bool>>>(),
            It.IsAny<CancellationToken>()))
            .Returns<Func<Task<bool>>, CancellationToken>(async (func, ct) => { await func(); });

        _mockUnitOfWork.Setup(x => x.SessionSaveChangesAsync())
            .Returns(Task.CompletedTask);

        _mockCourseCache.Setup(x => x.ClearCourseDetailForStudentCacheAsync())
            .Returns(Task.CompletedTask);

        _mockCourseCache.Setup(x => x.ClearEnrollmentStatusCacheAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        _mockCourseCache.Setup(x => x.ClearGetAllCacheAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _studentProgressService.EnrollCourseAsync(_courseId);

        // Assert
        Assert.That(result.Success, Is.True);
        // MessageId.I00001 with args "Người dùng đã tham gia khóa học thành công"
        Assert.That(result.Message, Contains.Substring("thành công"));

        _mockEnrollmentRepository.Verify(x => x.AddAsync(
            It.Is<CourseStudentEnrollment>(e => e.CourseId == _courseId && e.UserId == _userId),
            user.Email), Times.Once);
    }
}

