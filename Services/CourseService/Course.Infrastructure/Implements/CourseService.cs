using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Pagination;
using Course.Application.Courses.Commands.CreateCourse;
using Course.Application.Courses.Commands.EnrollCourse;
using Course.Application.Courses.Commands.UpdateCourse;
using Course.Application.Courses.Commands.UpdateCourseModules;
using Course.Application.Courses.Queries.CheckEnrollment;
using Course.Application.Courses.Queries.GetCourseById;
using Course.Application.Courses.Queries.GetCourseBySlug;
using Course.Application.Courses.Queries.GetCourses;
using Course.Application.Courses.Queries.GetCourseTags;
using Course.Application.DTOs.CoursesDTO;
using Course.Application.DTOs.CourseTagsDTO;
using Course.Application.DTOs.LessonsDTO;
using Course.Application.DTOs.ModulesDTO;
using Course.Application.DTOs.ModulesDTO.ModuleDiscussionDTO;
using Course.Application.DTOs.ModulesDTO.ModuleMaterialDTO;
using Course.Application.Interfaces;
using Course.Domain.Enum;
using Course.Domain.Models;
using Course.Domain.ReadModels;
using Course.Infrastructure.Caching;
using Course.Infrastructure.Extensions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Linq.Expressions;

namespace Course.Infrastructure.Implements
{
	public class CourseService(
		ICommandRepository<CourseEntity> _courseRepository,
		ICommandRepository<CourseStudentEnrollment> _enrollmentRepository,
		IQueryRepository<CourseStudentEnrollmentCollection> _enrollmentQueryRepository,
		ICommandRepository<Tag> _tagRepository,
		IUnitOfWork unitOfWork,
		IDatabase _cache,
		IIdentityService _identityService) : ICourseService
	{
		/// <summary>
		/// Get all courses with pagination and optional filtering
		/// </summary>
		/// <param name="pagination"></param>
		/// <param name="query"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<GetCoursesResponse> GetAllAsync(
			PaginationRequest pagination,
			CourseQuery? query = null,
			CancellationToken ct = default)
		{
			var response = new GetCoursesResponse() { Success = false };

			// Generate cache key based on pagination and query parameters
			var cacheKey = GenerateCacheKeyForGetAll(pagination, query);

			// Try to get from cache first
			var cached = await _cache.GetAsync<PaginatedResult<CourseDto>>(cacheKey);
			if (cached is not null)
			{
				response.Success = true;
				response.Message = "OK (from cache)";
				response.Response = cached;
				return response;
			}

			// Build predicate dynamic theo filter
			Expression<Func<CourseEntity, bool>>? predicate = null;

			if (query is not null)
			{
				// Start with 'true' and &=
				Expression<Func<CourseEntity, bool>> Acc(Expression<Func<CourseEntity, bool>> left,
					Expression<Func<CourseEntity, bool>> right)
					=> left is null ? right : left.AndAlso(right);

				// helpers
				Expression<Func<CourseEntity, bool>> True() => x => true;

				var pred = True();

				if (!string.IsNullOrWhiteSpace(query.Search))
				{
					var search = query.Search.Trim();
					pred = Acc(pred, x =>
						EF.Functions.ILike(x.Title ?? "", $"%{search}%") ||
						EF.Functions.ILike(x.Description ?? "", $"%{search}%") ||
						EF.Functions.ILike(x.ShortDescription ?? "", $"%{search}%") ||
						EF.Functions.ILike(x.Slug ?? "", $"%{search}%")
					);
				}

				if (!string.IsNullOrWhiteSpace(query.SubjectCode))
				{
					var code = query.SubjectCode.Trim();

					pred = Acc(pred, x => x.Subject != null &&
										  EF.Functions.ILike(x.Subject.SubjectCode ?? "", $"%{code}%"));
				}

				if (query.IsActive is bool isActive)
					pred = Acc(pred, x => x.IsActive == isActive);

				if (query.LectureId.HasValue)
					pred = Acc(pred, x => x.TeacherId == query.LectureId.Value);

				predicate = pred;
			}

			// Chọn orderBy mặc định theo COurse mới nhất
			Expression<Func<CourseEntity, object>> orderBy = x => x.UpdatedAt;

			var orderByDescending = true; // mặc định: mới nhất

			switch (query?.SortBy ?? CourseSortBy.Latest)
			{
				case CourseSortBy.Popular:
					orderBy = x => x.LearnerCount;
					orderByDescending = true;
					break;
				case CourseSortBy.TitleAsc:
					orderBy = x => x.Title!;
					orderByDescending = false;
					break;
				case CourseSortBy.TitleDesc:
					orderBy = x => x.Title!;
					orderByDescending = true;
					break;
				case CourseSortBy.Latest:
					orderBy = x => x.UpdatedAt;
					orderByDescending = true;
					break;
				default:
					orderByDescending = true;
					break;
			}

			// PaginationRequest đang 0-based (PageIndex). Chuyển sang 1-based để an toàn.
			var pageNumber = pagination.PageIndex + 1;
			var pageSize = pagination.PageSize;

			var page = await _courseRepository.PagedAsync(
				pageNumber: pageNumber,
				pageSize: pageSize,
				predicate: predicate,
				orderBy: orderBy,
				orderByDescending: orderByDescending,
				cancellationToken: ct,
				x => x.Subject
			// includes: nếu cần eager load, thêm tại đây: x => x.Subject, x => x.Modules ...
			);

			// Map entity -> DTO
			var items = page.Items.Select(MapToDto).ToList();

			var result = new PaginatedResult<CourseDto>(
				pageIndex: pagination.PageIndex,
				pageSize: pagination.PageSize,
				totalCount: page.TotalCount,
				data: items
			);

			// Cache the result for future requests
			await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

			response.Success = true;
			response.Response = result;
			response.SetMessage(MessageId.I00001, "Lấy danh sách khóa học");

			return response;
		}

		/// <summary>
		/// Get course details by ID for guest users
		/// </summary>
		/// <param name="Id"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<GetCourseByIdForGuestResponse> GetCourseByIdForGuestAsync(Guid Id, CancellationToken ct = default)
		{
			var response = new GetCourseByIdForGuestResponse() { Success = false };

			var cacheKey = $"CourseDetailForGuest:{Id}";
			var cached = await _cache.GetAsync<CourseDetailForGuestDto>(cacheKey);
			if (cached is not null)
			{
				response.Success = true;
				response.SetMessage(MessageId.I00001, "Lấy chi tiết khóa học cho khách");
				response.Response = cached;
				response.ModulesCount = cached.Modules.Count;
				response.LessonsCount = cached.Modules.Sum(m => m.Lessons.Count);
				return response;
			}

			var baseQuery = _courseRepository
				.Find(x => x.CourseId == Id && x.IsActive, isTracking: false, ct)
				.Cast<CourseEntity>()
				.Include(x => x.Subject)
				.Include(x => x.CourseObjectives.Where(o => o.IsActive))
				.Include(x => x.CourseRequirements.Where(r => r.IsActive))
				.Include(x => x.CourseComments.Where(c => c.IsActive))
				.Include(x => x.CourseTags).ThenInclude(ct => ct.Tag)
				.Include(x => x.CourseRatings)
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.ModuleObjectives.Where(o => o.IsActive))
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.Lessons.Where(l => l.IsActive));

			var entity = await baseQuery.FirstOrDefaultAsync(ct);

			if (entity is null)
			{
				response.SetMessage(MessageId.E00000, $"Không tìm thấy khóa học với mã {Id}");
				return response;
			}

			var detail = MapCourseDetailForGuest(entity);
			await _cache.SetAsync(cacheKey, detail, TimeSpan.FromMinutes(10));
			var modulesCount = entity.Modules.Count(m => m.IsActive);
			var lessonsCount = entity.Modules.Sum(m => m.Lessons.Count(l => l.IsActive));

			response.Success = true;
			response.SetMessage(MessageId.I00001, "Lấy chi tiết khóa học cho khách");
			response.Response = detail;
			response.ModulesCount = modulesCount;
			response.LessonsCount = lessonsCount;
			return response;
		}

		/// <summary>
		/// Get course details by Slug for guest users
		/// </summary>
		/// <param name="Slug"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public async Task<GetCourseBySlugForGuestResponse> GetCourseBySlugForGuestAsync(string Slug, CancellationToken ct = default)
		{
			var response = new GetCourseBySlugForGuestResponse() { Success = false };

			var cacheKey = $"CourseDetailBySlugForGuest:{Slug}";
			var cached = await _cache.GetAsync<CourseDetailForGuestDto>(cacheKey);
			if (cached is not null)
			{
				response.Success = true;
				response.SetMessage(MessageId.I00001, "Lấy chi tiết khóa học cho khách");
				response.Response = cached;
				response.ModulesCount = cached.Modules.Count;
				response.LessonsCount = cached.Modules.Sum(m => m.Lessons.Count);
				return response;
			}

			var baseQuery = _courseRepository
				.Find(x => x.Slug == Slug && x.IsActive, isTracking: false, ct)
				.Cast<CourseEntity>()
				.Include(x => x.Subject)
				.Include(x => x.CourseObjectives.Where(o => o.IsActive))
				.Include(x => x.CourseRequirements.Where(r => r.IsActive))
				.Include(x => x.CourseComments.Where(c => c.IsActive))
				.Include(x => x.CourseTags).ThenInclude(ct => ct.Tag)
				.Include(x => x.CourseRatings)
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.ModuleObjectives.Where(o => o.IsActive))
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.Lessons.Where(l => l.IsActive));

			var entity = await baseQuery.FirstOrDefaultAsync(ct);

			if (entity is null)
			{
				response.SetMessage(MessageId.E00000, $"Không tìm thấy khóa học với Slug {Slug}");
				return response;
			}

			var detail = MapCourseDetailForGuest(entity);
			await _cache.SetAsync(cacheKey, detail, TimeSpan.FromMinutes(10));
			var modulesCount = entity.Modules.Count(m => m.IsActive);
			var lessonsCount = entity.Modules.Sum(m => m.Lessons.Count(l => l.IsActive));

			response.Success = true;
			response.SetMessage(MessageId.I00001, "Lấy chi tiết khóa học cho khách");
			response.Response = detail;
			response.ModulesCount = modulesCount;
			response.LessonsCount = lessonsCount;
			return response;
		}

		/// <summary>
		/// Get course details by ID for lecturer (instructor) users
		/// </summary>
		/// <param name="id"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<GetCourseByIdForLectureResponse> GetCourseByIdForLectureAsync(Guid Id, CancellationToken ct = default)
		{
			var response = new GetCourseByIdForLectureResponse() { Success = false };

			var cacheKey = $"CourseDetailForLecture:{Id}";
			var cached = await _cache.GetAsync<CourseDetailForLectureDto>(cacheKey);
			if (cached is not null)
			{
				response.Success = true;
				response.SetMessage(MessageId.I00001, "Lấy chi tiết khóa học cho giảng viên");
				response.Response = cached;
				response.ModulesCount = cached.Modules.Count;
				response.LessonsCount = cached.Modules.Sum(m => m.Lessons.Count);
				return response;
			}

			var baseQuery = _courseRepository
				.Find(x => x.CourseId == Id && x.IsActive, isTracking: false, ct)
				.Cast<CourseEntity>()
				.Include(x => x.Subject)
				.Include(x => x.CourseObjectives.Where(o => o.IsActive))
				.Include(x => x.CourseRequirements.Where(r => r.IsActive))
				.Include(x => x.CourseComments.Where(c => c.IsActive))
				.Include(x => x.CourseTags).ThenInclude(ct => ct.Tag)
				.Include(x => x.CourseRatings)
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.ModuleObjectives.Where(o => o.IsActive))
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.ModuleDiscussions.Where(d => d.IsActive))
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.ModuleMaterials.Where(mat => mat.IsActive))
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.Lessons.Where(l => l.IsActive));

			var entity = await baseQuery.FirstOrDefaultAsync(ct);

			if (entity is null)
			{
				response.SetMessage(MessageId.E00000, $"Không tìm thấy khóa học với mã {Id}");
				return response;
			}

			var detail = MapCourseDetailForLecture(entity);
			await _cache.SetAsync(cacheKey, detail, TimeSpan.FromMinutes(10));
			var modulesCount = entity.Modules.Count(m => m.IsActive);
			var lessonsCount = entity.Modules.Sum(m => m.Lessons.Count(l => l.IsActive));

			response.Success = true;
			response.SetMessage(MessageId.I00001, "Lấy chi tiết khóa học cho giảng viên");
			response.Response = detail;
			response.ModulesCount = modulesCount;
			response.LessonsCount = lessonsCount;
			return response;
		}

		/// <summary>
		/// Get course details by Slug for lecturer (instructor) users
		/// </summary>
		/// <param name="Slug"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public async Task<GetCourseBySlugForLectureResponse> GetCourseBySlugForLectureAsync(string Slug, CancellationToken ct = default)
		{
			var response = new GetCourseBySlugForLectureResponse() { Success = false };

			var cacheKey = $"CourseDetailBySlugForLecture:{Slug}";
			var cached = await _cache.GetAsync<CourseDetailForLectureDto>(cacheKey);
			if (cached is not null)
			{
				response.Success = true;
				response.SetMessage(MessageId.I00001, "Lấy chi tiết khóa học cho giảng viên");
				response.Response = cached;
				response.ModulesCount = cached.Modules.Count;
				response.LessonsCount = cached.Modules.Sum(m => m.Lessons.Count);
				return response;
			}

			var baseQuery = _courseRepository
				.Find(x => x.Slug == Slug && x.IsActive, isTracking: false, ct)
				.Cast<CourseEntity>()
				.Include(x => x.Subject)
				.Include(x => x.CourseObjectives.Where(o => o.IsActive))
				.Include(x => x.CourseRequirements.Where(r => r.IsActive))
				.Include(x => x.CourseComments.Where(c => c.IsActive))
				.Include(x => x.CourseTags).ThenInclude(ct => ct.Tag)
				.Include(x => x.CourseRatings)
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.ModuleObjectives.Where(o => o.IsActive))
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.ModuleDiscussions.Where(d => d.IsActive))
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.ModuleMaterials.Where(mat => mat.IsActive))
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.Lessons.Where(l => l.IsActive));

			var entity = await baseQuery.FirstOrDefaultAsync(ct);

			if (entity is null)
			{
				response.SetMessage(MessageId.E00000, $"Không tìm thấy khóa học với Slug {Slug}");
				return response;
			}

			var detail = MapCourseDetailForLecture(entity);
			await _cache.SetAsync(cacheKey, detail, TimeSpan.FromMinutes(10));
			var modulesCount = entity.Modules.Count(m => m.IsActive);
			var lessonsCount = entity.Modules.Sum(m => m.Lessons.Count(l => l.IsActive));

			response.Success = true;
			response.SetMessage(MessageId.I00001, "Lấy chi tiết khóa học cho giảng viên");
			response.Response = detail;
			response.ModulesCount = modulesCount;
			response.LessonsCount = lessonsCount;
			return response;
		}

		/// <summary>
		/// Check if current user is enrolled in a course
		/// </summary>
		/// <param name="courseId"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<CheckEnrollmentResponse> CheckEnrollmentAsync(Guid courseId, CancellationToken ct = default)
		{
			var response = new CheckEnrollmentResponse() { Success = false };

			// Get current user id from token
			var currentUser = _identityService.GetCurrentUser()!;

			// Check if user is enrolled in the course
			//var enrollment = await _enrollmentRepository
			//	.Find(x => x.CourseId == courseId && x.UserId == currentUser.UserId && x.IsActive, isTracking: false, ct)
			//	.FirstOrDefaultAsync(ct);

			var cacheKey = $"enroll:status:{currentUser.UserId}:{courseId}";

			var enrollment = await _enrollmentQueryRepository.GetOrSetAsync(
				cacheKey,
				() => _enrollmentQueryRepository.FirstOrDefaultAsync(x =>
					x.CourseId == courseId &&
					x.UserId == currentUser.UserId &&
					x.IsActive),
				TimeSpan.FromMinutes(5)
			);

			if (enrollment is null)
			{
				response.Success = true;
				response.SetMessage(MessageId.I00000, "Người dùng chưa tham gia khóa học");
				response.Response = false;
				return response;
			}

			response.Success = true;
			response.SetMessage(MessageId.I00001, "Người dùng đã tham gia khóa học");
			response.Response = true;

			return response;
		}

		/// <summary>
		/// Enroll current user in a course
		/// </summary>
		/// <param name="courseId"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public async Task<EnrollInCourseResponse> EnrollCourseAsync(Guid courseId, CancellationToken ct = default)
		{
			var response = new EnrollInCourseResponse() { Success = false };

			// Get current user id from token
			var currentUser = _identityService.GetCurrentUser()!;

			if (currentUser is null)
			{
				response.SetMessage(MessageId.E00000, "Người dùng chưa đăng nhập");
				return response;
			}

			// Check if user is already enrolled in the course
			var existingEnrollment = await _enrollmentRepository
				.Find(x => x.CourseId == courseId && x.UserId == currentUser.UserId && x.IsActive, isTracking: false, ct)
				.FirstOrDefaultAsync(ct);

			if (existingEnrollment is not null)
			{
				response.SetMessage(MessageId.E11004, "Người dùng đã tham gia khóa học");
				return response;
			}

			// Create new enrollment
			var enrollment = new CourseStudentEnrollment
			{
				EnrollmentId = Guid.NewGuid(),
				CourseId = courseId,
				UserId = currentUser.UserId,
				StartedAt = DateTime.UtcNow,
				ExpiresAt = null,
				IsActive = true
			};

			// Save to database within a transaction
			await unitOfWork.BeginTransactionAsync(async () =>
						{
							await _enrollmentRepository.AddAsync(enrollment, currentUser.Email);
							await unitOfWork.SaveChangesAsync(ct);

							unitOfWork.Store(CourseStudentEnrollmentCollection.FromWriteModel(enrollment));
							await unitOfWork.SessionSaveChangesAsync();
							return true;
						}, ct);

			// Respond success
			response.Success = true;
			response.SetMessage(MessageId.I00001, "Người dùng đã tham gia khóa học thành công");

			return response;
		}

		/// <summary>
		/// Create course with modules + lessons
		/// </summary>
		/// <param name="dto"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<CreateCourseResponse> CreateAsync(CreateCourseDto dto, CancellationToken ct = default)
		{
			var response = new CreateCourseResponse() { Success = false };

			// Get current user id
			var currentUser = _identityService.GetCurrentUser();

			if (currentUser is null)
			{
				currentUser = new IdentityEntity
				{
					UserId = Guid.Empty,
					FullName = "system",
					Email = "system"
				};
			}

			var title = dto.Title?.Trim() ?? string.Empty;

			// TODO: fix logic validate (khi nhập slug trong quá trình Create)
			var slug = !string.IsNullOrWhiteSpace(dto.Slug) ? dto.Slug.Trim() : await GenerateUniqueSlugAsync(dto.Title!, ct);

			var course = new CourseEntity
			{
				CourseId = Guid.NewGuid(),
				TeacherId = dto.TeacherId,
				SubjectId = dto.SubjectId,
				Title = title,
				ShortDescription = dto.ShortDescription,
				Description = dto.Description,
				Slug = slug,
				CourseImageUrl = dto.CourseImageUrl,
				//Status = dto.Status,          // nếu enum: dto.Status; nếu string: giữ nguyên
				LearnerCount = 0,
				DurationMinutes = dto.DurationMinutes,
				Level = dto.Level,
				Price = dto.Price,
				DealPrice = dto.DealPrice,
				CourseIntroVideoUrl = dto.CourseIntroVideoUrl,
			};

			// 3) Mục tiêu học tập (CourseObjectives) – optional
			if (dto.Objectives is { Count: > 0 })
			{
				foreach (var (obj, idx) in dto.Objectives
							 .OrderBy(o => o.PositionIndex)
							 .Select((o, i) => (o, i)))
				{
					course.CourseObjectives.Add(new CourseObjective
					{
						ObjectiveId = Guid.NewGuid(),
						CourseId = course.CourseId,
						Content = obj.Content,
						PositionIndex = obj.PositionIndex > 0 ? obj.PositionIndex : idx
					});
				}
			}

			// 4) Yêu cầu trước khi học (CourseRequirements) – optional
			if (dto.Requirements is { Count: > 0 })
			{
				foreach (var (req, idx) in dto.Requirements
							 .OrderBy(r => r.PositionIndex)
							 .Select((r, i) => (r, i)))
				{
					course.CourseRequirements.Add(new CourseRequirement
					{
						RequirementId = Guid.NewGuid(),
						CourseId = course.CourseId,
						Content = req.Content,
						PositionIndex = req.PositionIndex > 0 ? req.PositionIndex : idx
					});
				}
			}

			// 5) Course Tags – optional
			if (dto.CourseTags is { Count: > 0 })
			{
				foreach (var courseTag in dto.CourseTags)
				{
					course.CourseTags.Add(new CourseTag
					{
						CourseId = course.CourseId,
						TagId = courseTag.TagId,
					});
				}
			}
			if (dto.Modules is { Count: > 0 })
			{
				// 6) Map Modules + Lessons (giữ thứ tự PositionIndex)
				foreach (var m in dto.Modules.OrderBy(x => x.PositionIndex))
				{
					var module = new Module
					{
						ModuleId = Guid.NewGuid(),
						CourseId = course.CourseId,
						ModuleName = m.ModuleName,
						Description = m.Description,
						PositionIndex = m.PositionIndex,
						IsCore = m.IsCore,
						DurationMinutes = m.DurationMinutes,
						Level = m.Level,
						IsActive = true
					};

					// Module Objectives (optional)
					if (m.Objectives is { Count: > 0 })
					{
						foreach (var (mo, idx) in m.Objectives
									 .OrderBy(o => o.PositionIndex)
									 .Select((o, i) => (o, i)))
						{
							module.ModuleObjectives.Add(new ModuleObjective
							{
								ObjectiveId = Guid.NewGuid(),
								ModuleId = module.ModuleId,
								Content = mo.Content,
								PositionIndex = mo.PositionIndex > 0 ? mo.PositionIndex : idx,
								IsActive = true
							});
						}
					}

					// Lessons (required, at least 1)
					if (m.Lessons is { Count: > 0 })
					{
						foreach (var l in m.Lessons.OrderBy(x => x.PositionIndex))
						{
							module.Lessons.Add(new Lesson
							{
								LessonId = Guid.NewGuid(),
								ModuleId = module.ModuleId,
								Title = l.Title,
								VideoUrl = l.VideoUrl,
								VideoDurationSec = l.VideoDurationSec,
								PositionIndex = l.PositionIndex,
								IsActive = true
							});
						}
					}

					// Discussions (optional)
					if (m.Discussions is { Count: > 0 })
					{
						foreach (var d in m.Discussions)
						{
							module.ModuleDiscussions.Add(new ModuleDiscussion
							{
								DiscussionId = Guid.NewGuid(),
								ModuleId = module.ModuleId,
								Title = d.Title?.Trim(),
								Description = d.Description,
								DiscussionQuestion = d.DiscussionQuestion,
								IsActive = true
							});
						}
					}

					// Materials (optional)
					if (m.Materials is { Count: > 0 })
					{
						foreach (var mat in m.Materials)
						{
							module.ModuleMaterials.Add(new ModuleMaterial
							{
								MaterialId = Guid.NewGuid(),
								ModuleId = module.ModuleId,
								Title = mat.Title?.Trim(),
								Description = mat.Description,
								FileUrl = mat.FileUrl,
								IsActive = true
							});
						}
					}

					course.Modules.Add(module);
				}
			}

			await unitOfWork.BeginTransactionAsync(async () =>
						{
							await _courseRepository.AddAsync(course, currentUser.Email);
							await unitOfWork.SaveChangesAsync(ct);

							return true; // yêu cầu của BeginTransactionAsync: trả true để commit
						}, ct);

			// Clear cache after successful creation
			await ClearGetAllCacheAsync();
			await ClearCourseTagsCacheAsync();

			response.Response = course.CourseId.ToString();
			response.Success = true;
			response.SetMessage(MessageId.I00000, "Tạo khóa học thành công");

			return response;
		}

		/// <summary>
		/// Update course and its related data (objectives, requirements)
		/// </summary>
		/// <param name="courseId"></param>
		/// <param name="dto"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<UpdateCourseResponse> UpdateAsync(Guid courseId, UpdateCourseDto dto, CancellationToken ct = default)
		{
			var response = new UpdateCourseResponse() { Success = false };
			// 1. Validate PositionIndex uniqueness
			ValidatePositionIndexes(dto);

			// 2. Get existing course with all related data
			var existingCourse = await _courseRepository
				.Find(x => x.CourseId == courseId, isTracking: true, ct,
					x => x.CourseObjectives,
					x => x.CourseRequirements)
				.FirstOrDefaultAsync(ct);

			if (existingCourse is null)
			{
				response.Message = $"Course {courseId} not found";
				return response;
			}

			var currentUser = _identityService.GetCurrentUser();

			if (currentUser is null)
			{
				currentUser = new IdentityEntity
				{
					UserId = Guid.Empty,
					FullName = "system",
					Email = "system"
				};
			}

			// 3. Update basic course properties
			existingCourse.TeacherId = dto.TeacherId;
			existingCourse.SubjectId = dto.SubjectId;
			existingCourse.Title = dto.Title?.Trim() ?? string.Empty;
			existingCourse.ShortDescription = dto.ShortDescription;
			existingCourse.Description = dto.Description;
			existingCourse.CourseImageUrl = dto.CourseImageUrl;
			existingCourse.DurationMinutes = dto.DurationMinutes;
			existingCourse.Level = dto.Level;
			existingCourse.Price = dto.Price;
			existingCourse.DealPrice = dto.DealPrice;
			existingCourse.IsActive = dto.IsActive;

			// 4. Handle slug update with uniqueness check
			if (!string.IsNullOrWhiteSpace(dto.Slug))
			{
				var newSlug = dto.Slug.Trim();
				if (newSlug != existingCourse.Slug)
				{
					existingCourse.Slug = await EnsureUniqueSlugForUpdateAsync(courseId, newSlug, ct, allowRandomSuffix: true);
				}
			}

			// 5. Update CourseObjectives
			await UpdateCourseObjectivesAsync(existingCourse, dto.Objectives, currentUser.Email);

			// 6. Update CourseRequirements
			await UpdateCourseRequirementsAsync(existingCourse, dto.Requirements, currentUser.Email);

			// 7. Save changes in transaction
			await unitOfWork.BeginTransactionAsync(async () =>
			{
				_courseRepository.Update(existingCourse, currentUser.Email);
				await unitOfWork.SaveChangesAsync(ct);
				return true;
			}, ct);

			// 8. Clear cache after successful update
			await ClearGetAllCacheAsync();
			await ClearCourseTagsCacheAsync();

			// 9. Return updated course detail
			response.Success = true;
			response.SetMessage(MessageId.I00001, "Cập nhật khóa học thành công");
			return response;
		}

		/// <summary>
		/// Update multiple modules in a course (bulk update)
		/// </summary>
		/// <param name="courseId"></param>
		/// <param name="dto"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<UpdateCourseModulesResponse> UpdateCourseModulesAsync(Guid courseId, UpdateCourseModulesDto dto, CancellationToken ct = default)
		{
			var response = new UpdateCourseModulesResponse() { Success = false };
			// 1. Validate PositionIndex uniqueness across all modules
			ValidateCourseModulesPositionIndexes(dto);

			// 2. Get existing course with all modules and related data
			var existingCourse = await _courseRepository
				.Find(x => x.CourseId == courseId, isTracking: true, ct,
					x => x.Modules)
				.Include(x => x.Modules)
					.ThenInclude(m => m.ModuleObjectives)
				.Include(x => x.Modules)
					.ThenInclude(m => m.Lessons)
				.FirstOrDefaultAsync(ct);

			if (existingCourse is null)
			{
				response.Message = $"Course {courseId} not found";
				return response;
			}

			var currentUser = _identityService.GetCurrentUser();

			if (currentUser is null)
			{
				currentUser = new IdentityEntity
				{
					UserId = Guid.Empty,
					FullName = "system",
					Email = "system"
				};
			}

			// 3. Update modules based on payload
			await UpdateCourseModulesInternalAsync(existingCourse, dto.Modules, currentUser.Email);

			// 4. Save changes in transaction
			await unitOfWork.BeginTransactionAsync(async () =>
			{
				_courseRepository.Update(existingCourse, currentUser.Email);
				await unitOfWork.SaveChangesAsync(ct);
				return true;
			}, ct);

			// 5. Clear cache after successful update
			await ClearGetAllCacheAsync();
			await ClearCourseTagsCacheAsync();

			// 6. Return response
			response.Success = true;
			response.SetMessage(MessageId.I00001, "Cập nhật các module của khóa học");

			return response;
		}

		/// <summary>
		/// Get all course tags
		/// </summary>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<GetCourseTagsResponse> GetCourseTagsAsync(CancellationToken ct = default)
		{
			var response = new GetCourseTagsResponse() { Success = false };

			// generate cache key for course tags
			var cacheKey = "CourseTags:GetAll";

			// get from cache first
			var cached = await _cache.GetAsync<List<CourseTagDetailsDto>>(cacheKey);
			if (cached is not null)
			{
				response.Success = true;
				response.SetMessage(MessageId.I00001, "Lấy danh sách tag của khóa học");
				response.Response = cached;
				return response;
			}

			// get from database
			var tags = await _tagRepository
				.Find(predicate: null, isTracking: false, cancellationToken: ct)
				.Select(t => new CourseTagDetailsDto(
					t.TagId,
					t.TagName
				))
				.ToListAsync(ct);

			// Cache the result for future requests
			await _cache.SetAsync(cacheKey, tags, TimeSpan.FromMinutes(10));

			response.Success = true;
			response.SetMessage(MessageId.I00001, "Lấy danh sách tag của khóa học");
			response.Response = tags;

			return response;
		}



		#region Private Helper Methods

		/// <summary>
		/// Generate cache key for GetAllAsync method based on pagination and query parameters
		/// </summary>
		/// <param name="pagination"></param>
		/// <param name="query"></param>
		/// <returns></returns>
		private static string GenerateCacheKeyForGetAll(PaginationRequest pagination, CourseQuery? query)
		{
			var keyParts = new List<string> { "Courses:GetAll" };

			// Add pagination parameters
			keyParts.Add($"PageIndex:{pagination.PageIndex}");
			keyParts.Add($"PageSize:{pagination.PageSize}");

			// Add query parameters if present
			if (query is not null)
			{
				if (!string.IsNullOrWhiteSpace(query.Search))
					keyParts.Add($"Search:{query.Search.Trim().ToLowerInvariant()}");

				if (!string.IsNullOrWhiteSpace(query.SubjectCode))
					keyParts.Add($"SubjectCode:{query.SubjectCode.Trim().ToLowerInvariant()}");

				if (query.IsActive.HasValue)
					keyParts.Add($"IsActive:{query.IsActive.Value}");

				if (query.LectureId.HasValue)
					keyParts.Add($"TeacherId:{query.LectureId.Value}");

				keyParts.Add($"SortBy:{query.SortBy}");
			}

			return string.Join(":", keyParts);
		}

		/// <summary>
		/// Clear cache for GetAllAsync method when course data changes
		/// </summary>
		/// <returns></returns>
		private async Task ClearGetAllCacheAsync()
		{
			// Get all cache keys that start with "Courses:GetAll"
			var server = _cache.Multiplexer.GetServer(_cache.Multiplexer.GetEndPoints().FirstOrDefault()!);
			var keys = server.Keys(pattern: "Courses:GetAll*");

			if (keys.Any())
			{
				await _cache.KeyDeleteAsync(keys.ToArray());
			}
		}

		/// <summary>
		/// Clear cache for GetCourseTagsAsync method when tag data changes
		/// </summary>
		/// <returns></returns>
		private async Task ClearCourseTagsCacheAsync()
		{
			// Get all cache keys that start with "CourseTags:"
			var server = _cache.Multiplexer.GetServer(_cache.Multiplexer.GetEndPoints().FirstOrDefault()!);
			var keys = server.Keys(pattern: "CourseTags:*");

			if (keys.Any())
			{
				await _cache.KeyDeleteAsync(keys.ToArray());
			}
		}

		/// <summary>
		/// Generate slug from title
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		private static string ToSlug(string input)
		{
			if (string.IsNullOrWhiteSpace(input)) return Guid.NewGuid().ToString("n")[..8];
			var s = input.ToLowerInvariant().Trim();
			s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", "-");
			s = System.Text.RegularExpressions.Regex.Replace(s, @"[^a-z0-9\-]", "");
			s = System.Text.RegularExpressions.Regex.Replace(s, "-{2,}", "-").Trim('-');
			return string.IsNullOrWhiteSpace(s) ? Guid.NewGuid().ToString("n")[..8] : s;
		}

		/// <summary>
		/// Generate a unique slug by appending a random suffix if necessary
		/// </summary>
		/// <param name="title"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		private async Task<string> GenerateUniqueSlugAsync(string title, CancellationToken ct)
		{
			var baseSlug = ToSlug(title);
			var slug = baseSlug;

			while (await _courseRepository.Find(c => c.Slug == slug).AnyAsync(ct))
			{
				// lấy 6 ký tự ngẫu nhiên từ Guid
				var suffix = Guid.NewGuid().ToString("N")[..6];
				slug = $"{baseSlug}-{suffix}";
			}

			return slug;
		}

		// Đảm bảo slug unique khi UPDATE (bỏ qua chính course hiện tại).
		// Nếu allowRandomSuffix=true, khi trùng sẽ gắn thêm -xxxxxx (6 hex) để tránh loop nhiều lần.
		private async Task<string> EnsureUniqueSlugForUpdateAsync(Guid courseId, string candidate, CancellationToken ct, bool allowRandomSuffix = false)
		{
			var slug = ToSlug(candidate);

			// Nếu slug đã thuộc về chính course này -> ok
			var ownedByCurrent = await _courseRepository
				.Find(c => c.CourseId == courseId && c.Slug == slug)
				.AnyAsync(ct);
			if (ownedByCurrent) return slug;

			var exists = await _courseRepository
				.Find(c => c.Slug == slug && c.CourseId != courseId)
				.AnyAsync(ct);

			if (!exists) return slug;

			if (allowRandomSuffix)
			{
				var suffix = Guid.NewGuid().ToString("N")[..6];
				return await EnsureUniqueSlugForUpdateAsync(courseId, $"{slug}-{suffix}", ct, allowRandomSuffix: false);
			}
			else
			{
				// fallback: tăng dần -1, -2 ... (hiếm khi cần)
				var baseSlug = slug;
				var i = 1;
				while (await _courseRepository.Find(c => c.Slug == slug && c.CourseId != courseId).AnyAsync(ct))
				{
					slug = $"{baseSlug}-{i}";
					i++;
				}
				return slug;
			}
		}

		/// <summary>
		/// Map CourseEntity -> CourseDetailDto
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		private static CourseDetailForGuestDto MapCourseDetailForGuest(CourseEntity e)
		{
			var modules = e.Modules
				.OrderBy(m => m.PositionIndex)
				.Select(m => new ModuleDetailDto<GuestLessonDetailDto>(
					m.ModuleId,
					m.ModuleName,
					m.Description,
					m.PositionIndex,
					m.IsActive,
					m.IsCore,
					m.DurationMinutes,
					m.DurationHours,
					m.Level,
					m.ModuleObjectives
						.OrderBy(o => o.PositionIndex)
						.Select(o => new ModuleObjectiveDto(
							o.ObjectiveId,
							o.Content,
							o.PositionIndex,
							o.IsActive
						)).ToList(),
					m.Lessons
						.OrderBy(l => l.PositionIndex)
						.Select(l => new GuestLessonDetailDto(
							l.LessonId,
							l.Title,
							l.PositionIndex,
							l.IsActive))
						.ToList()
				)).ToList();

			// Comments
			var comments = e.CourseComments
							.OrderBy(c => c.CreatedAt)
							.Select(c => new CourseCommentDto(
								c.CommentId,
								c.UserId,
								c.Content,
								c.ParentCommentId,
								c.CreatedAt,
								c.IsActive
							)).ToList();

			// Tags
			var tags = e.CourseTags
				.Select(t => new CourseTagDto(
					t.TagId,
					t.Tag?.TagName ?? string.Empty
				)).ToList();

			// Ratings + thống kê
			var ratings = e.CourseRatings
				.OrderByDescending(r => r.CreatedAt)
				.Select(r => new CourseRatingDto(
					r.RatingId,
					r.UserId,
					r.Rating,
					r.CreatedAt
				)).ToList();

			var ratingsCount = ratings.Count;
			var ratingsAverage = ratingsCount > 0
				? Math.Round(e.CourseRatings.Average(r => r.Rating), 2)
				: 0.0;

			return new CourseDetailForGuestDto(
				e.CourseId,
				e.TeacherId,
				e.SubjectId,
				e.Subject?.SubjectCode ?? string.Empty,
				e.Title ?? string.Empty,
				e.ShortDescription,
				e.Description,
				e.Slug,
				e.CourseImageUrl,
				e.LearnerCount,
				e.DurationMinutes,
				e.DurationHours,
				e.Level,
				e.Price,
				e.DealPrice,
				e.IsActive,
				e.CreatedAt,
				e.UpdatedAt,
				e.CourseObjectives
					.OrderBy(o => o.PositionIndex)
					.Select(o => new CourseObjectiveDto(o.ObjectiveId, o.Content, o.PositionIndex, o.IsActive))
					.ToList(),
				e.CourseRequirements
					.OrderBy(r => r.PositionIndex)
					.Select(r => new CourseRequirementDto(r.RequirementId, r.Content, r.PositionIndex, r.IsActive))
					.ToList(),
				modules,
				comments,
				tags,
				ratings,
				ratingsCount,
				//ratingsAverage
				5.0
			);
		}

		/// <summary>
		/// Map CourseEntity -> CourseDetailDto
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		private static CourseDetailForLectureDto MapCourseDetailForLecture(CourseEntity e)
		{
			var modules = e.Modules
				.Where(m => m.IsActive)
				.OrderBy(m => m.PositionIndex)
				.Select(m => new ModuleDetailForLectureDto(
					m.ModuleId,
					m.ModuleName,
					m.Description,
					m.PositionIndex,
					m.IsActive,
					m.IsCore,
					m.DurationMinutes,
					m.DurationHours,
					m.Level,
					m.ModuleObjectives
						.Where(o => o.IsActive)
						.OrderBy(o => o.PositionIndex)
						.Select(o => new ModuleObjectiveDto(
							o.ObjectiveId,
							o.Content,
							o.PositionIndex,
							o.IsActive
						)).ToList(),
					m.ModuleDiscussions
						.Where(d => d.IsActive)
						.Select(d => new ModuleDiscussionDetailDto(
						d.DiscussionId,
						d.Title,
						d.Description,
						d.DiscussionQuestion,
						d.CreatedAt,
						d.UpdatedAt
					)).ToList(),
					m.ModuleMaterials
						.Where(mat => mat.IsActive)
						.Select(mat => new ModuleMaterialDetailDto(
						mat.MaterialId,
						mat.Title,
						mat.Description,
						mat.FileUrl,
						mat.CreatedAt,
						mat.UpdatedAt
					)).ToList(),
					m.Lessons
						.Where(l => l.IsActive)
						.OrderBy(l => l.PositionIndex)
						.Select(l => new LectureLessonDetailDto(
							l.LessonId,
							l.Title,
							l.VideoUrl,
							l.VideoDurationSec,
							l.PositionIndex,
							l.IsActive))
						.ToList()
				)).ToList();

			// Comments
			var comments = e.CourseComments
							.OrderBy(c => c.CreatedAt)
							.Select(c => new CourseCommentDto(
								c.CommentId,
								c.UserId,
								c.Content,
								c.ParentCommentId,
								c.CreatedAt,
								c.IsActive
							)).ToList();

			// Tags
			var tags = e.CourseTags
				.Select(t => new CourseTagDto(
					t.TagId,
					t.Tag?.TagName ?? string.Empty
				)).ToList();

			// Ratings + thống kê
			var ratings = e.CourseRatings
				.OrderByDescending(r => r.CreatedAt)
				.Select(r => new CourseRatingDto(
					r.RatingId,
					r.UserId,
					r.Rating,
					r.CreatedAt
				)).ToList();

			var ratingsCount = ratings.Count;
			var ratingsAverage = ratingsCount > 0
				? Math.Round(e.CourseRatings.Average(r => r.Rating), 2)
				: 0.0;

			return new CourseDetailForLectureDto(
				e.CourseId,
				e.TeacherId,
				e.SubjectId,
				e.Subject?.SubjectCode ?? string.Empty,
				e.Title ?? string.Empty,
				e.ShortDescription,
				e.Description,
				e.Slug,
				e.CourseImageUrl,
				e.LearnerCount,
				e.Modules.SelectMany(m => m.Lessons)
					.OrderBy(l => l.PositionIndex)
					.FirstOrDefault()?.VideoUrl ?? string.Empty,
				e.Modules.SelectMany(m => m.Lessons)
				.OrderBy(l => l.PositionIndex)
					.FirstOrDefault()?.VideoDurationSec ?? 0,
				e.DurationMinutes,
				e.DurationHours,
				e.Level,
				e.Price,
				e.DealPrice,
				e.IsActive,
				e.CreatedAt,
				e.UpdatedAt,
				e.CourseObjectives
					.OrderBy(o => o.PositionIndex)
					.Select(o => new CourseObjectiveDto(o.ObjectiveId, o.Content, o.PositionIndex, o.IsActive))
					.ToList(),
				e.CourseRequirements
					.OrderBy(r => r.PositionIndex)
					.Select(r => new CourseRequirementDto(r.RequirementId, r.Content, r.PositionIndex, r.IsActive))
					.ToList(),
				modules,
				comments,
				tags,
				ratings,
				ratingsCount,
				//ratingsAverage
				5.0
			);
		}

		/// <summary>
		/// Map CourseEntity -> CourseDto
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		private static CourseDto MapToDto(CourseEntity e) => new(
			CourseId: e.CourseId,
			TeacherId: e.TeacherId,
			SubjectId: e.SubjectId,
			SubjectCode: e.Subject?.SubjectCode ?? string.Empty,
			Title: e.Title ?? string.Empty,
			ShortDescription: e.ShortDescription,
			Description: e.Description,
			Slug: e.Slug,
			CourseImageUrl: e.CourseImageUrl,
			LearnerCount: e.LearnerCount,
			DurationMinutes: e.DurationMinutes,
			DurationHours: e.DurationHours,
			Level: e.Level,
			Price: e.Price,
			DealPrice: e.DealPrice,
			IsActive: e.IsActive,
			CreatedAt: e.CreatedAt,
			UpdatedAt: e.UpdatedAt
		);

		/// <summary>
		/// Validate PositionIndex uniqueness across all modules in UpdateCourseModulesDto
		/// </summary>
		/// <param name="dto"></param>
		/// <exception cref="ValidationException"></exception>
		private static void ValidatePositionIndexes(UpdateCourseDto dto)
		{
			// Course Objectives (chỉ active)
			if (dto.Objectives is { Count: > 0 })
			{
				var activeIdx = dto.Objectives
					.Where(o => o.IsActive)
					.Select(o => o.PositionIndex)
					.ToList();

				if (activeIdx.Count != activeIdx.Distinct().Count())
					throw new ValidationException("Objective PositionIndex must be unique among active objectives.");

				if (activeIdx.Any(i => i <= 0))
					throw new ValidationException("Objective PositionIndex must be > 0 for active objectives.");
			}

			// Course Requirements (chỉ active)
			if (dto.Requirements is { Count: > 0 })
			{
				var activeIdx = dto.Requirements
					.Where(r => r.IsActive)
					.Select(r => r.PositionIndex)
					.ToList();

				if (activeIdx.Count != activeIdx.Distinct().Count())
					throw new ValidationException("Requirement PositionIndex must be unique among active requirements.");

				if (activeIdx.Any(i => i < 0))
					throw new ValidationException("Requirement PositionIndex must be >= 0 for active requirements.");
			}
		}

		/// <summary>
		/// Update CourseObjectives based on payload
		/// </summary>
		private Task UpdateCourseObjectivesAsync(CourseEntity course, List<UpdateCourseObjectiveDto>? objectives, string actor)
		{
			if (objectives is null || objectives.Count == 0)
			{
				// Mark all existing objectives as inactive (soft delete)
				foreach (var obj in course.CourseObjectives.Where(o => o.IsActive))
				{
					obj.IsActive = false;
				}
				return Task.CompletedTask;
			}

			var now = DateTime.UtcNow;
			var existingObjectives = course.CourseObjectives.ToDictionary(o => o.ObjectiveId, o => o);
			var payloadObjectiveIds = objectives.Where(o => o.ObjectiveId.HasValue).Select(o => o.ObjectiveId!.Value).ToHashSet();

			// 1. Mark objectives not in payload as inactive (soft delete)
			foreach (var existing in existingObjectives.Values.Where(o => o.IsActive && !payloadObjectiveIds.Contains(o.ObjectiveId)))
			{
				existing.IsActive = false;
			}

			// 2. Update existing objectives or create new ones
			foreach (var objDto in objectives)
			{
				if (objDto.ObjectiveId.HasValue && existingObjectives.TryGetValue(objDto.ObjectiveId.Value, out var existing))
				{
					// Update existing objective
					existing.Content = objDto.Content;
					existing.PositionIndex = objDto.PositionIndex;
					existing.IsActive = objDto.IsActive;
				}
				else
				{
					// Create new objective - let EF generate the ID
					var newObjective = new CourseObjective
					{
						CourseId = course.CourseId,
						Content = objDto.Content,
						PositionIndex = objDto.PositionIndex,
						IsActive = objDto.IsActive,
						CreatedAt = now,
						UpdatedAt = now,
						CreatedBy = actor,
						UpdatedBy = actor
					};
					course.CourseObjectives.Add(newObjective);
				}
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Update CourseRequirements based on payload
		/// </summary>
		private Task UpdateCourseRequirementsAsync(CourseEntity course, List<UpdateCourseRequirementDto>? requirements, string actor)
		{
			if (requirements is null || requirements.Count == 0)
			{
				// Mark all existing requirements as inactive (soft delete)
				foreach (var req in course.CourseRequirements.Where(r => r.IsActive))
				{
					req.IsActive = false;
				}
				return Task.CompletedTask;
			}

			var now = DateTime.UtcNow;
			var existingRequirements = course.CourseRequirements.ToDictionary(r => r.RequirementId, r => r);
			var payloadRequirementIds = requirements.Where(r => r.RequirementId.HasValue).Select(r => r.RequirementId!.Value).ToHashSet();

			// 1. Mark requirements not in payload as inactive (soft delete)
			foreach (var existing in existingRequirements.Values.Where(r => r.IsActive && !payloadRequirementIds.Contains(r.RequirementId)))
			{
				existing.IsActive = false;
			}

			// 2. Update existing requirements or create new ones
			foreach (var reqDto in requirements)
			{
				if (reqDto.RequirementId.HasValue && existingRequirements.TryGetValue(reqDto.RequirementId.Value, out var existing))
				{
					// Update existing requirement
					existing.Content = reqDto.Content;
					existing.PositionIndex = reqDto.PositionIndex;
					existing.IsActive = reqDto.IsActive;
				}
				else
				{
					// Create new requirement - let EF generate the ID
					var newRequirement = new CourseRequirement
					{
						CourseId = course.CourseId,
						Content = reqDto.Content,
						PositionIndex = reqDto.PositionIndex,
						IsActive = reqDto.IsActive,
						CreatedAt = now,
						UpdatedAt = now,
						CreatedBy = actor,
						UpdatedBy = actor
					};
					course.CourseRequirements.Add(newRequirement);
				}
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Validate PositionIndex uniqueness across all modules in course
		/// </summary>
		private static void ValidateCourseModulesPositionIndexes(UpdateCourseModulesDto dto)
		{
			if (dto.Modules is null || dto.Modules.Count == 0)
				return;

			// Validate module PositionIndex uniqueness
			var moduleIndexes = dto.Modules
				.Where(m => m.IsActive)
				.Select(m => m.PositionIndex)
				.ToList();

			if (moduleIndexes.Count != moduleIndexes.Distinct().Count())
				throw new ValidationException("Module PositionIndex must be unique within the course.");

			if (moduleIndexes.Any(i => i <= 0))
				throw new ValidationException("Module PositionIndex must be > 0 for active modules.");

			// Validate objectives and lessons within each module
			foreach (var module in dto.Modules.Where(m => m.IsActive))
			{
				// Module Objectives
				if (module.Objectives is { Count: > 0 })
				{
					var activeObjIdx = module.Objectives
						.Where(o => o.IsActive)
						.Select(o => o.PositionIndex)
						.ToList();

					if (activeObjIdx.Count != activeObjIdx.Distinct().Count())
						throw new ValidationException($"Module '{module.ModuleName}' objectives' PositionIndex must be unique.");

					if (activeObjIdx.Any(i => i <= 0))
						throw new ValidationException($"Module '{module.ModuleName}' objectives' PositionIndex must be > 0 for active objectives.");
				}

				// Lessons
				if (module.Lessons is { Count: > 0 })
				{
					var activeLessonIdx = module.Lessons
						.Where(l => l.IsActive)
						.Select(l => l.PositionIndex)
						.ToList();

					if (activeLessonIdx.Count != activeLessonIdx.Distinct().Count())
						throw new ValidationException($"Module '{module.ModuleName}' lessons' PositionIndex must be unique.");

					if (activeLessonIdx.Any(i => i <= 0))
						throw new ValidationException($"Module '{module.ModuleName}' lessons' PositionIndex must be > 0 for active lessons.");
				}
			}
		}

		/// <summary>
		/// Internal method to update course modules
		/// </summary>
		private async Task UpdateCourseModulesInternalAsync(CourseEntity course, List<UpdateCourseModuleDto> modules, string actor)
		{
			if (modules is null || modules.Count == 0)
			{
				// Mark all existing modules as inactive (soft delete)
				foreach (var module in course.Modules.Where(m => m.IsActive))
				{
					module.IsActive = false;
				}
				return;
			}

			var existingModules = course.Modules.ToDictionary(m => m.ModuleId, m => m);
			var payloadModuleIds = modules.Where(m => m.ModuleId.HasValue).Select(m => m.ModuleId!.Value).ToHashSet();

			// 1. Mark modules not in payload as inactive (soft delete)
			foreach (var existing in existingModules.Values.Where(m => m.IsActive && !payloadModuleIds.Contains(m.ModuleId)))
			{
				existing.IsActive = false;
			}

			// 2. Update existing modules or create new ones
			foreach (var moduleDto in modules)
			{
				if (moduleDto.ModuleId.HasValue && existingModules.TryGetValue(moduleDto.ModuleId.Value, out var existingModule))
				{
					// Update existing module
					await UpdateExistingModuleAsync(existingModule, moduleDto, actor);
				}
				else
				{
					// Create new module
					await CreateNewModuleAsync(course, moduleDto, actor);
				}
			}
		}

		/// <summary>
		/// Update existing module with its objectives and lessons
		/// </summary>
		private async Task UpdateExistingModuleAsync(Module existingModule, UpdateCourseModuleDto moduleDto, string actor)
		{
			// Update basic module properties
			existingModule.ModuleName = moduleDto.ModuleName?.Trim() ?? string.Empty;
			existingModule.Description = moduleDto.Description;
			existingModule.PositionIndex = moduleDto.PositionIndex;
			existingModule.IsActive = moduleDto.IsActive;
			existingModule.IsCore = moduleDto.IsCore;
			existingModule.DurationMinutes = moduleDto.DurationMinutes;
			existingModule.Level = moduleDto.Level;

			// Update ModuleObjectives
			await UpdateModuleObjectivesInternalAsync(existingModule, moduleDto.Objectives, actor);

			// Update Lessons
			await UpdateLessonsInternalAsync(existingModule, moduleDto.Lessons, actor);
		}

		/// <summary>
		/// Create new module with its objectives and lessons
		/// </summary>
		private Task CreateNewModuleAsync(CourseEntity course, UpdateCourseModuleDto moduleDto, string actor)
		{
			var now = DateTime.UtcNow;

			var newModule = new Module
			{
				CourseId = course.CourseId,
				ModuleName = moduleDto.ModuleName?.Trim() ?? string.Empty,
				Description = moduleDto.Description,
				PositionIndex = moduleDto.PositionIndex,
				IsActive = moduleDto.IsActive,
				IsCore = moduleDto.IsCore,
				DurationMinutes = moduleDto.DurationMinutes,
				Level = moduleDto.Level,
				CreatedAt = now,
				UpdatedAt = now,
				CreatedBy = actor,
				UpdatedBy = actor
			};

			// Add ModuleObjectives
			if (moduleDto.Objectives is { Count: > 0 })
			{
				foreach (var objDto in moduleDto.Objectives.OrderBy(o => o.PositionIndex))
				{
					newModule.ModuleObjectives.Add(new ModuleObjective
					{
						ModuleId = newModule.ModuleId,
						Content = objDto.Content,
						PositionIndex = objDto.PositionIndex,
						IsActive = objDto.IsActive,
						CreatedAt = now,
						UpdatedAt = now,
						CreatedBy = actor,
						UpdatedBy = actor
					});
				}
			}

			// Add Lessons
			if (moduleDto.Lessons is { Count: > 0 })
			{
				foreach (var lessonDto in moduleDto.Lessons.OrderBy(l => l.PositionIndex))
				{
					newModule.Lessons.Add(new Lesson
					{
						ModuleId = newModule.ModuleId,
						Title = lessonDto.Title,
						VideoUrl = lessonDto.VideoUrl,
						VideoDurationSec = lessonDto.VideoDurationSec,
						PositionIndex = lessonDto.PositionIndex,
						IsActive = lessonDto.IsActive,
						CreatedAt = now,
						UpdatedAt = now,
						CreatedBy = actor,
						UpdatedBy = actor
					});
				}
			}

			course.Modules.Add(newModule);

			return Task.CompletedTask;
		}

		/// <summary>
		/// Update ModuleObjectives for existing module
		/// </summary>
		private Task UpdateModuleObjectivesInternalAsync(Module module, List<UpdateCourseModuleObjectiveDto>? objectives, string actor)
		{
			if (objectives is null || objectives.Count == 0)
			{
				// Mark all existing objectives as inactive (soft delete)
				foreach (var obj in module.ModuleObjectives.Where(o => o.IsActive))
				{
					obj.IsActive = false;
				}
				return Task.CompletedTask;
			}

			var now = DateTime.UtcNow;
			var existingObjectives = module.ModuleObjectives.ToDictionary(o => o.ObjectiveId, o => o);
			var payloadObjectiveIds = objectives.Where(o => o.ObjectiveId.HasValue).Select(o => o.ObjectiveId!.Value).ToHashSet();

			// 1. Mark objectives not in payload as inactive (soft delete)
			foreach (var existing in existingObjectives.Values.Where(o => o.IsActive && !payloadObjectiveIds.Contains(o.ObjectiveId)))
			{
				existing.IsActive = false;
			}

			// 2. Update existing objectives or create new ones
			foreach (var objDto in objectives)
			{
				if (objDto.ObjectiveId.HasValue && existingObjectives.TryGetValue(objDto.ObjectiveId.Value, out var existing))
				{
					// Update existing objective
					existing.Content = objDto.Content;
					existing.PositionIndex = objDto.PositionIndex;
					existing.IsActive = objDto.IsActive;
				}
				else
				{
					// Create new objective - let EF generate the ID
					var newObjective = new ModuleObjective
					{
						ModuleId = module.ModuleId,
						Content = objDto.Content,
						PositionIndex = objDto.PositionIndex,
						IsActive = objDto.IsActive,
						CreatedAt = now,
						UpdatedAt = now,
						CreatedBy = actor,
						UpdatedBy = actor
					};
					module.ModuleObjectives.Add(newObjective);
				}
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Update Lessons for existing module
		/// </summary>
		private Task UpdateLessonsInternalAsync(Module module, List<UpdateCourseLessonDto> lessons, string actor)
		{
			if (lessons is null || lessons.Count == 0)
			{
				// Mark all existing lessons as inactive (soft delete)
				foreach (var lesson in module.Lessons.Where(l => l.IsActive))
				{
					lesson.IsActive = false;
				}
				return Task.CompletedTask;
			}

			var now = DateTime.UtcNow;
			var existingLessons = module.Lessons.ToDictionary(l => l.LessonId, l => l);
			var payloadLessonIds = lessons.Where(l => l.LessonId.HasValue).Select(l => l.LessonId!.Value).ToHashSet();

			// 1. Mark lessons not in payload as inactive (soft delete)
			foreach (var existing in existingLessons.Values.Where(l => l.IsActive && !payloadLessonIds.Contains(l.LessonId)))
			{
				existing.IsActive = false;
			}

			// 2. Update existing lessons or create new ones
			foreach (var lessonDto in lessons)
			{
				if (lessonDto.LessonId.HasValue && existingLessons.TryGetValue(lessonDto.LessonId.Value, out var existing))
				{
					// Update existing lesson
					existing.Title = lessonDto.Title;
					existing.VideoUrl = lessonDto.VideoUrl;
					existing.VideoDurationSec = lessonDto.VideoDurationSec;
					existing.PositionIndex = lessonDto.PositionIndex;
					existing.IsActive = lessonDto.IsActive;
				}
				else
				{
					// Create new lesson - let EF generate the ID
					var newLesson = new Lesson
					{
						ModuleId = module.ModuleId,
						Title = lessonDto.Title,
						VideoUrl = lessonDto.VideoUrl,
						VideoDurationSec = lessonDto.VideoDurationSec,
						PositionIndex = lessonDto.PositionIndex,
						IsActive = lessonDto.IsActive,
						CreatedAt = now,
						UpdatedAt = now,
						CreatedBy = actor,
						UpdatedBy = actor
					};
					module.Lessons.Add(newLesson);
				}
			}
			return Task.CompletedTask;
		}
		#endregion
	}
}
