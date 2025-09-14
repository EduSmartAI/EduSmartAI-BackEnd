using BaseService.Application.Interfaces.Repositories;
using BuildingBlocks.Pagination;
using Course.Application.Courses.Commands.CreateCourse;
using Course.Application.Courses.Commands.UpdateCourse;
using Course.Application.Courses.Queries.GetCourseById;
using Course.Application.Courses.Queries.GetCourses;
using Course.Application.DTOs.CoursesDTO;
using Course.Application.DTOs.LessonsDTO;
using Course.Application.DTOs.Modules;
using Course.Application.Interfaces;
using Course.Domain.Enum;
using Course.Domain.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Course.Infrastructure.Implements
{
	public class CourseService(ICourseRepository _courseRepository, IUnitOfWork unitOfWork) : ICourseService
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
					// cần Include(x => x.Subject) ở query phía dưới
					pred = Acc(pred, x => x.Subject != null &&
										  EF.Functions.ILike(x.Subject.SubjectCode ?? "", $"%{code}%"));
				}

				if (query.IsActive is bool isActive)
					pred = Acc(pred, x => x.IsActive == isActive);

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
				default:
					orderBy = x => x.UpdatedAt;
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

			return new GetCoursesResponse
			{
				Success = true,
				Response = result,
				Message = "OK"
			};
		}

		/// <summary>
		/// Create course with modules + lessons
		/// </summary>
		/// <param name="dto"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<CreateCourseResponse> CreateAsync(CreateCourseDto dto, CancellationToken ct = default)
		{
			var title = dto.Title?.Trim() ?? string.Empty;

			// TODO: fix logic validate (khi nhập slug trong quá trình Create)
			var slug = !string.IsNullOrWhiteSpace(dto.Slug) ? dto.Slug.Trim() : await GenerateUniqueSlugAsync(dto.Title, ct);
			var now = DateTime.UtcNow;
			const string actor = "system"; // TODO: inject IUserContext để lấy username thực

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
				LearnerCount = 0,                   // mặc định
				DurationMinutes = dto.DurationMinutes,
				Level = dto.Level,
				Price = dto.Price,
				DealPrice = dto.DealPrice,
				IsActive = dto.IsActive,
				CreatedAt = now,
				UpdatedAt = now,
				CreatedBy = actor,
				UpdatedBy = actor
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
						PositionIndex = obj.PositionIndex > 0 ? obj.PositionIndex : idx, // fallback trật tự
						IsActive = obj.IsActive,
						CreatedAt = now,
						UpdatedAt = now,
						CreatedBy = actor,
						UpdatedBy = actor
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
						PositionIndex = req.PositionIndex > 0 ? req.PositionIndex : idx,
						IsActive = req.IsActive,
						CreatedAt = now,
						UpdatedAt = now,
						CreatedBy = actor,
						UpdatedBy = actor
					});
				}
			}

			// Map Modules + Lessons (giữ thứ tự PositionIndex)
			foreach (var m in dto.Modules.OrderBy(x => x.PositionIndex))
			{
				var module = new Module
				{
					ModuleId = Guid.NewGuid(),
					CourseId = course.CourseId,
					ModuleName = m.ModuleName,
					Description = m.Description,
					PositionIndex = m.PositionIndex,
					IsActive = m.IsActive,
					IsCore = m.IsCore,
					DurationMinutes = m.DurationMinutes,
					Level = m.Level,
					CreatedAt = now,
					UpdatedAt = now,
					CreatedBy = actor,
					UpdatedBy = actor
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
							IsActive = mo.IsActive,
							CreatedAt = now,
							UpdatedAt = now,
							CreatedBy = actor,
							UpdatedBy = actor
						});
					}
				}

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
						IsActive = l.IsActive,
						CreatedAt = now,
						UpdatedAt = now,
						CreatedBy = actor,
						UpdatedBy = actor
					});
				}

				course.Modules.Add(module);
			}

			await unitOfWork.BeginTransactionAsync(async () =>
						{
							await _courseRepository.AddAsync(course, actor);   // hoặc Insert/Add tùy interface bạn đang dùng
							await unitOfWork.SaveChangesAsync(ct);         // EF: SaveChanges; Marten: cũng qua UoW
																		   // Nếu có Outbox/Event:
																		   // _uow.Store(new CourseCreatedEvent { CourseId = course.CourseId, ... });
																		   // await _uow.SessionSaveChangesAsync();

							return true; // yêu cầu của BeginTransactionAsync: trả true để commit
						}, ct);

			return new CreateCourseResponse
			{
				Success = true,
				Message = "Course created successfully"
			};
			//return MapDetail(course);
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
		private static CourseDetailDto MapDetail(CourseEntity e)
		{
			var modules = e.Modules
				.OrderBy(m => m.PositionIndex)
				.Select(m => new ModuleDetailDto(
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
							//l.VideoUrl,
							//l.VideoDurationSec,
							l.PositionIndex,
							l.IsActive))
						.ToList()
				)).ToList();

			return new CourseDetailDto(
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
				modules
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

		/*
		 | Thực thể | Điều kiện payload  |                  Tồn tại trong DB | Hành động    |
		 | -------- | ------------------ | --------------------------------: | ------------ |
		 | Module   | `moduleId == null` |                                 — | **Thêm mới** |
		 | Module   | `moduleId != null` |                   Có trong course | **Cập nhật** |
		 | Module   | (Bất kỳ)           | Không còn xuất hiện trong payload | **Xóa**      |
		 | Lesson   | `lessonId == null` |                                 — | **Thêm mới** |
		 | Lesson   | `lessonId != null` |                   Có trong module | **Cập nhật** |
		 | Lesson   | (Bất kỳ)           | Không còn xuất hiện trong payload | **Xóa**      |
		 */
		public async Task<UpdateCourseResponse> UpdateAsync(Guid courseId, UpdateCourseDto dto, CancellationToken ct = default)
		{
			// 1. Validate PositionIndex uniqueness
			ValidatePositionIndexes(dto);

			// 2. Get existing course with all related data
			var existingCourse = await _courseRepository
				.Find(x => x.CourseId == courseId, isTracking: true, ct,
					x => x.CourseObjectives,
					x => x.CourseRequirements)
				.FirstOrDefaultAsync(ct);

			if (existingCourse is null)
				return new UpdateCourseResponse
				{
					Success = false,
					Message = $"Course {courseId} not found"
				};

			const string actor = "system"; // TODO: inject IUserContext để lấy username thực

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
			await UpdateCourseObjectivesAsync(existingCourse, dto.Objectives, actor, ct);

			// 6. Update CourseRequirements
			await UpdateCourseRequirementsAsync(existingCourse, dto.Requirements, actor, ct);

			// 7. Save changes in transaction
			await unitOfWork.BeginTransactionAsync(async () =>
			{
				_courseRepository.Update(existingCourse, actor);
				await unitOfWork.SaveChangesAsync(ct);
				return true;
			}, ct);

			// 8. Return updated course detail
			//var updatedCourse = await GetByIdAsync(courseId, ct);
			return new UpdateCourseResponse
			{
				Success = true,
				Message = "Course updated successfully",
			};
		}

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
		private async Task UpdateCourseObjectivesAsync(CourseEntity course, List<UpdateCourseObjectiveDto>? objectives, string actor, CancellationToken ct)
		{
			if (objectives is null || objectives.Count == 0)
			{
				// Mark all existing objectives as inactive (soft delete)
				foreach (var obj in course.CourseObjectives.Where(o => o.IsActive))
				{
					obj.IsActive = false;
					obj.UpdatedAt = DateTime.UtcNow;
					obj.UpdatedBy = actor;
				}
				return;
			}

			var now = DateTime.UtcNow;
			var existingObjectives = course.CourseObjectives.ToDictionary(o => o.ObjectiveId, o => o);
			var payloadObjectiveIds = objectives.Where(o => o.ObjectiveId.HasValue).Select(o => o.ObjectiveId!.Value).ToHashSet();

			// 1. Mark objectives not in payload as inactive (soft delete)
			foreach (var existing in existingObjectives.Values.Where(o => o.IsActive && !payloadObjectiveIds.Contains(o.ObjectiveId)))
			{
				existing.IsActive = false;
				existing.UpdatedAt = now;
				existing.UpdatedBy = actor;
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
					existing.UpdatedAt = now;
					existing.UpdatedBy = actor;
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
		}

		/// <summary>
		/// Update CourseRequirements based on payload
		/// </summary>
		private async Task UpdateCourseRequirementsAsync(CourseEntity course, List<UpdateCourseRequirementDto>? requirements, string actor, CancellationToken ct)
		{
			if (requirements is null || requirements.Count == 0)
			{
				// Mark all existing requirements as inactive (soft delete)
				foreach (var req in course.CourseRequirements.Where(r => r.IsActive))
				{
					req.IsActive = false;
					req.UpdatedAt = DateTime.UtcNow;
					req.UpdatedBy = actor;
				}
				return;
			}

			var now = DateTime.UtcNow;
			var existingRequirements = course.CourseRequirements.ToDictionary(r => r.RequirementId, r => r);
			var payloadRequirementIds = requirements.Where(r => r.RequirementId.HasValue).Select(r => r.RequirementId!.Value).ToHashSet();

			// 1. Mark requirements not in payload as inactive (soft delete)
			foreach (var existing in existingRequirements.Values.Where(r => r.IsActive && !payloadRequirementIds.Contains(r.RequirementId)))
			{
				existing.IsActive = false;
				existing.UpdatedAt = now;
				existing.UpdatedBy = actor;
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
					existing.UpdatedAt = now;
					existing.UpdatedBy = actor;
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
		}

		public async Task<GetCourseByIdResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
		{
			var baseQuery = _courseRepository
				.Find(x => x.CourseId == id, isTracking: false, ct)
				.Cast<CourseEntity>()
				.Include(x => x.Subject)
				.Include(x => x.CourseObjectives)
				.Include(x => x.CourseRequirements)
				.Include(x => x.Modules).ThenInclude(m => m.ModuleObjectives)
				.Include(x => x.Modules).ThenInclude(m => m.Lessons);

			var entity = await baseQuery.FirstOrDefaultAsync(ct);

			if (entity is null)
				return new GetCourseByIdResponse { Success = false, Message = $"Course {id} not found" };

			var detail = MapDetail(entity);
			var modulesCount = entity.Modules.Count(m => m.IsActive);
			var lessonsCount = entity.Modules.Sum(m => m.Lessons.Count(l => l.IsActive));

			return new GetCourseByIdResponse
			{
				Success = true,
				Message = "OK",
				Response = detail,
				ModulesCount = modulesCount,
				LessonsCount = lessonsCount
			};
		}
	}

	// Expression helper để AND các biểu thức
	internal static class ExpressionExtensions
	{
		public static Expression<Func<T, bool>> AndAlso<T>(
			this Expression<Func<T, bool>> expr1,
			Expression<Func<T, bool>> expr2)
		{
			var parameter = Expression.Parameter(typeof(T));

			var leftVisitor = new ReplaceExpressionVisitor(expr1.Parameters[0], parameter);
			var left = leftVisitor.Visit(expr1.Body)!;

			var rightVisitor = new ReplaceExpressionVisitor(expr2.Parameters[0], parameter);
			var right = rightVisitor.Visit(expr2.Body)!;

			return Expression.Lambda<Func<T, bool>>(
				Expression.AndAlso(left, right), parameter);
		}

		private sealed class ReplaceExpressionVisitor : ExpressionVisitor
		{
			private readonly Expression _oldValue;
			private readonly Expression _newValue;

			public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
				=> (_oldValue, _newValue) = (oldValue, newValue);

			public override Expression? Visit(Expression? node)
				=> node == _oldValue ? _newValue : base.Visit(node);
		}
	}
}
