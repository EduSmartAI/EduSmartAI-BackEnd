using BaseService.Application.Interfaces.Repositories;
using BuildingBlocks.Pagination;
using Course.Application.Courses.Commands.CreateCourse;
using Course.Application.Courses.Commands.UpdateCourse;
using Course.Application.Courses.Queries.GetCourseById;
using Course.Application.Courses.Queries.GetCourses;
using Course.Application.DTOs;
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
						.Select(l => new LessonDetailDto(
							l.LessonId,
							l.Title,
							l.VideoUrl,
							l.VideoDurationSec,
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
			const string actor = "system";
			var now = DateTime.UtcNow;

			// ========== LOAD GRAPH ==========
			var course = await _courseRepository
				.Find(c => c.CourseId == courseId, isTracking: true, ct,
					c => c.Modules,
					c => c.CourseObjectives,
					c => c.CourseRequirements)
				.Include(c => c.Modules).ThenInclude(m => m.Lessons)
				.Include(c => c.Modules).ThenInclude(m => m.ModuleObjectives)
				.FirstOrDefaultAsync(ct)
				?? throw new KeyNotFoundException($"Course {courseId} not found");

			// ========== VALIDATION ==========
			ValidatePositionIndexes(dto);

			// ========== UPDATE ROOT ==========
			var oldTitle = course.Title ?? string.Empty;
			course.TeacherId = dto.TeacherId;
			course.SubjectId = dto.SubjectId;
			course.Title = dto.Title;
			course.ShortDescription = dto.ShortDescription;
			course.Description = dto.Description;
			course.CourseImageUrl = dto.CourseImageUrl;
			course.Price = dto.Price;
			course.DealPrice = dto.DealPrice;
			course.Level = dto.Level;
			course.DurationMinutes = dto.DurationMinutes;
			course.IsActive = dto.IsActive;
			course.UpdatedAt = now;
			course.UpdatedBy = actor;

			// Slug unique
			if (!string.IsNullOrWhiteSpace(dto.Slug))
			{
				var normalized = dto.Slug.Trim();
				course.Slug = await EnsureUniqueSlugForUpdateAsync(courseId, normalized, ct);
			}
			else if (!string.Equals(oldTitle, dto.Title, StringComparison.Ordinal))
			{
				course.Slug = await EnsureUniqueSlugForUpdateAsync(courseId, ToSlug(dto.Title), ct, allowRandomSuffix: true);
			}

			// Chuẩn hoá input lists
			var dtoObjectives = (dto.Objectives ?? new()).OrderBy(x => x.PositionIndex).ToList();
			var dtoRequirements = (dto.Requirements ?? new()).OrderBy(x => x.PositionIndex).ToList();
			var dtoModules = (dto.Modules ?? new()).OrderBy(x => x.PositionIndex).ToList();

			try
			{
				// ========== TRANSACTION ==========
				await unitOfWork.BeginTransactionAsync(async () =>
				{
					// ===================== PHASE A: DEACTIVATE (soft-delete) =====================

					// A1) Course Objectives
					var keepObjIds = dtoObjectives.Where(o => o.ObjectiveId.HasValue).Select(o => o.ObjectiveId!.Value).ToHashSet();
					foreach (var o in course.CourseObjectives.Where(x => x.IsActive && !keepObjIds.Contains(x.ObjectiveId)))
					{
						o.IsActive = false;
						o.UpdatedAt = now; o.UpdatedBy = actor;
					}

					// A2) Course Requirements
					var keepReqIds = dtoRequirements.Where(r => r.RequirementId.HasValue).Select(r => r.RequirementId!.Value).ToHashSet();
					foreach (var r in course.CourseRequirements.Where(x => x.IsActive && !keepReqIds.Contains(x.RequirementId)))
					{
						r.IsActive = false;
						r.UpdatedAt = now; r.UpdatedBy = actor;
					}

					// A3) Modules (vắng trong payload) + con của chúng
					var keepModuleIds = dtoModules.Where(m => m.ModuleId.HasValue).Select(m => m.ModuleId!.Value).ToHashSet();
					foreach (var m in course.Modules.Where(x => x.IsActive && !keepModuleIds.Contains(x.ModuleId)))
					{
						m.IsActive = false;
						m.UpdatedAt = now; m.UpdatedBy = actor;

						foreach (var l in m.Lessons.Where(x => x.IsActive))
						{
							l.IsActive = false;
							l.UpdatedAt = now; l.UpdatedBy = actor;
						}
						foreach (var mo in m.ModuleObjectives.Where(x => x.IsActive))
						{
							mo.IsActive = false;
							mo.UpdatedAt = now; mo.UpdatedBy = actor;
						}
					}

					// A4) Deactivate objectives/lessons bị remove bên trong các module còn giữ
					foreach (var mDto in dtoModules.Where(x => x.ModuleId.HasValue))
					{
						var module = course.Modules.First(x => x.ModuleId == mDto.ModuleId.Value);

						var keepMoIds = (mDto.Objectives ?? new()).Where(o => o.ObjectiveId.HasValue).Select(o => o.ObjectiveId!.Value).ToHashSet();
						foreach (var mo in module.ModuleObjectives.Where(x => x.IsActive && !keepMoIds.Contains(x.ObjectiveId)))
						{
							mo.IsActive = false;
							mo.UpdatedAt = now; mo.UpdatedBy = actor;
						}

						var keepLessonIds = (mDto.Lessons ?? new()).Where(l => l.LessonId.HasValue).Select(l => l.LessonId!.Value).ToHashSet();
						foreach (var l in module.Lessons.Where(x => x.IsActive && !keepLessonIds.Contains(x.LessonId)))
						{
							l.IsActive = false;
							l.UpdatedAt = now; l.UpdatedBy = actor;
						}
					}

					// ===================== PHASE B: UPSERT/ADD =====================

					// B1) Course Objectives (upsert)
					// FIX: Only consider objectives that will be kept (have ObjectiveId in DTO)
					var keptObjIds = dtoObjectives.Where(o => o.ObjectiveId.HasValue).Select(o => o.ObjectiveId!.Value).ToHashSet();
					var takenObjIdx = course.CourseObjectives
						.Where(x => x.IsActive && keptObjIds.Contains(x.ObjectiveId))
						.Select(x => x.PositionIndex)
						.ToHashSet();
					foreach (var oDto in dtoObjectives)
					{
						var pos = oDto.PositionIndex > 0 ? oDto.PositionIndex : NextIndex(takenObjIdx);
						// FIX: Ensure unique position index
						while (takenObjIdx.Contains(pos)) pos++;
						takenObjIdx.Add(pos);

						if (oDto.ObjectiveId is null)
						{
							course.CourseObjectives.Add(new CourseObjective
							{
								ObjectiveId = Guid.NewGuid(),
								CourseId = course.CourseId,
								Content = oDto.Content,
								PositionIndex = pos,
								IsActive = oDto.IsActive,
								CreatedAt = now,
								UpdatedAt = now,
								CreatedBy = actor,
								UpdatedBy = actor
							});
						}
						else
						{
							var obj = course.CourseObjectives.First(x => x.ObjectiveId == oDto.ObjectiveId.Value);
							obj.Content = oDto.Content;
							obj.PositionIndex = pos;
							obj.IsActive = oDto.IsActive;
							obj.UpdatedAt = now; obj.UpdatedBy = actor;
						}
					}

					// B2) Course Requirements (upsert)
					// FIX: Only consider requirements that will be kept (have RequirementId in DTO)
					var keptReqIds = dtoRequirements.Where(r => r.RequirementId.HasValue).Select(r => r.RequirementId!.Value).ToHashSet();
					var takenReqIdx = course.CourseRequirements
						.Where(x => x.IsActive && keptReqIds.Contains(x.RequirementId))
						.Select(x => x.PositionIndex)
						.ToHashSet();
					foreach (var rDto in dtoRequirements)
					{
						var pos = rDto.PositionIndex > 0 ? rDto.PositionIndex : NextIndex(takenReqIdx);
						// FIX: Ensure unique position index
						while (takenReqIdx.Contains(pos)) pos++;
						takenReqIdx.Add(pos);

						if (rDto.RequirementId is null)
						{
							course.CourseRequirements.Add(new CourseRequirement
							{
								RequirementId = Guid.NewGuid(),
								CourseId = course.CourseId,
								Content = rDto.Content,
								PositionIndex = pos,
								IsActive = rDto.IsActive,
								CreatedAt = now,
								UpdatedAt = now,
								CreatedBy = actor,
								UpdatedBy = actor
							});
						}
						else
						{
							var req = course.CourseRequirements.First(x => x.RequirementId == rDto.RequirementId.Value);
							req.Content = rDto.Content;
							req.PositionIndex = pos;
							req.IsActive = rDto.IsActive;
							req.UpdatedAt = now; req.UpdatedBy = actor;
						}
					}

					// B3) Modules (upsert)
					// FIX: Only consider modules that will be kept (have ModuleId in DTO)
					var keptModuleIds = dtoModules.Where(m => m.ModuleId.HasValue).Select(m => m.ModuleId!.Value).ToHashSet();
					var takenModuleIdx = course.Modules
						.Where(x => x.IsActive && keptModuleIds.Contains(x.ModuleId))
						.Select(x => x.PositionIndex)
						.ToHashSet();
					foreach (var mDto in dtoModules)
					{
						if (mDto.ModuleId is null)
						{
							var pos = mDto.PositionIndex > 0 ? mDto.PositionIndex : NextIndex(takenModuleIdx);
							// FIX: Ensure unique position index
							while (takenModuleIdx.Contains(pos)) pos++;
							takenModuleIdx.Add(pos);

							var newModule = new Module
							{
								ModuleId = Guid.NewGuid(),
								CourseId = course.CourseId,
								ModuleName = mDto.ModuleName,
								Description = mDto.Description,
								PositionIndex = pos,
								IsActive = mDto.IsActive,
								IsCore = mDto.IsCore,
								DurationMinutes = mDto.DurationMinutes,
								Level = mDto.Level,
								CreatedAt = now,
								UpdatedAt = now,
								CreatedBy = actor,
								UpdatedBy = actor
							};

							// Module Objectives
							var takenMoIdx = new HashSet<int>(); // FIX: Start with empty set for new module
							foreach (var moDto in (mDto.Objectives ?? new()).OrderBy(x => x.PositionIndex))
							{
								var mpos = moDto.PositionIndex > 0 ? moDto.PositionIndex : NextIndex(takenMoIdx);
								// FIX: Ensure unique position index
								while (takenMoIdx.Contains(mpos)) mpos++;
								takenMoIdx.Add(mpos);

								newModule.ModuleObjectives.Add(new ModuleObjective
								{
									ObjectiveId = Guid.NewGuid(),
									ModuleId = newModule.ModuleId,
									Content = moDto.Content,
									PositionIndex = mpos,
									IsActive = moDto.IsActive,
									CreatedAt = now,
									UpdatedAt = now,
									CreatedBy = actor,
									UpdatedBy = actor
								});
							}

							// Lessons
							var takenLessonIdx = new HashSet<int>(); // FIX: Start with empty set for new module
							foreach (var lDto in (mDto.Lessons ?? new()).OrderBy(x => x.PositionIndex))
							{
								var lpos = lDto.PositionIndex > 0 ? lDto.PositionIndex : NextIndex(takenLessonIdx);
								// FIX: Ensure unique position index
								while (takenLessonIdx.Contains(lpos)) lpos++;
								takenLessonIdx.Add(lpos);

								newModule.Lessons.Add(new Lesson
								{
									LessonId = Guid.NewGuid(),
									ModuleId = newModule.ModuleId,
									Title = lDto.Title,
									VideoUrl = lDto.VideoUrl,
									VideoDurationSec = lDto.VideoDurationSec,
									PositionIndex = lpos,
									IsActive = lDto.IsActive,
									CreatedAt = now,
									UpdatedAt = now,
									CreatedBy = actor,
									UpdatedBy = actor
								});
							}

							course.Modules.Add(newModule);
						}
						else
						{
							var module = course.Modules.First(x => x.ModuleId == mDto.ModuleId.Value);

							var pos = mDto.PositionIndex > 0 ? mDto.PositionIndex : NextIndex(takenModuleIdx);
							// FIX: Ensure unique position index
							while (takenModuleIdx.Contains(pos)) pos++;
							takenModuleIdx.Add(pos);

							module.ModuleName = mDto.ModuleName;
							module.Description = mDto.Description;
							module.PositionIndex = pos;
							module.IsActive = mDto.IsActive;
							module.IsCore = mDto.IsCore;
							module.DurationMinutes = mDto.DurationMinutes;
							module.Level = mDto.Level;
							module.UpdatedAt = now; module.UpdatedBy = actor;

							// Module Objectives (upsert)
							var dtoModObjs = (mDto.Objectives ?? new()).OrderBy(x => x.PositionIndex).ToList();
							// FIX: Only consider objectives that will be kept (have ObjectiveId in DTO)
							var keptMoIds = dtoModObjs.Where(o => o.ObjectiveId.HasValue).Select(o => o.ObjectiveId!.Value).ToHashSet();
							var takenMoIdx = module.ModuleObjectives
								.Where(x => x.IsActive && keptMoIds.Contains(x.ObjectiveId))
								.Select(x => x.PositionIndex)
								.ToHashSet();

							foreach (var moDto in dtoModObjs)
							{
								var mpos = moDto.PositionIndex > 0 ? moDto.PositionIndex : NextIndex(takenMoIdx);
								// FIX: Ensure unique position index
								while (takenMoIdx.Contains(mpos)) mpos++;
								takenMoIdx.Add(mpos);

								if (moDto.ObjectiveId is null)
								{
									module.ModuleObjectives.Add(new ModuleObjective
									{
										ObjectiveId = Guid.NewGuid(),
										ModuleId = module.ModuleId,
										Content = moDto.Content,
										PositionIndex = mpos,
										IsActive = moDto.IsActive,
										CreatedAt = now,
										UpdatedAt = now,
										CreatedBy = actor,
										UpdatedBy = actor
									});
								}
								else
								{
									var mo = module.ModuleObjectives.First(x => x.ObjectiveId == moDto.ObjectiveId.Value);
									mo.Content = moDto.Content;
									mo.PositionIndex = mpos;
									mo.IsActive = moDto.IsActive;
									mo.UpdatedAt = now; mo.UpdatedBy = actor;
								}
							}

							// Lessons (upsert)
							var dtoLessons = (mDto.Lessons ?? new()).OrderBy(x => x.PositionIndex).ToList();
							// FIX: Only consider lessons that will be kept (have LessonId in DTO)
							var keptLessonIds = dtoLessons.Where(l => l.LessonId.HasValue).Select(l => l.LessonId!.Value).ToHashSet();
							var takenLessonIdx = module.Lessons
								.Where(x => x.IsActive && keptLessonIds.Contains(x.LessonId))
								.Select(x => x.PositionIndex)
								.ToHashSet();

							foreach (var lDto in dtoLessons)
							{
								var lpos = lDto.PositionIndex > 0 ? lDto.PositionIndex : NextIndex(takenLessonIdx);
								// FIX: Ensure unique position index
								while (takenLessonIdx.Contains(lpos)) lpos++;
								takenLessonIdx.Add(lpos);

								if (lDto.LessonId is null)
								{
									module.Lessons.Add(new Lesson
									{
										LessonId = Guid.NewGuid(),
										ModuleId = module.ModuleId,
										Title = lDto.Title,
										VideoUrl = lDto.VideoUrl,
										VideoDurationSec = lDto.VideoDurationSec,
										PositionIndex = lpos,
										IsActive = lDto.IsActive,
										CreatedAt = now,
										UpdatedAt = now,
										CreatedBy = actor,
										UpdatedBy = actor
									});
								}
								else
								{
									var lesson = module.Lessons.First(x => x.LessonId == lDto.LessonId.Value);
									lesson.Title = lDto.Title;
									lesson.VideoUrl = lDto.VideoUrl;
									lesson.VideoDurationSec = lDto.VideoDurationSec;
									lesson.PositionIndex = lpos;
									lesson.IsActive = lDto.IsActive;
									lesson.UpdatedAt = now; lesson.UpdatedBy = actor;
								}
							}
						}
					}

					// FIX: Only save once at the end of transaction
					await unitOfWork.SaveChangesAsync(ct);

					return true;
				}, ct);

				return new UpdateCourseResponse
				{
					Success = true,
					Response = MapDetail(course),
					Message = "Course updated successfully"
				};
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return new UpdateCourseResponse
				{
					Success = false,
					Response = null,
					Message = $"Error updating course: {ex.Message}"
				};
			}
		}


		private static int NextIndex(ISet<int> taken)
		{
			var next = (taken.Count == 0 ? 1 : taken.Max() + 1);
			while (taken.Contains(next)) next++;
			return next;
		}

		private static void ValidatePositionIndexes(UpdateCourseDto dto)
		{
			// Course Objectives
			if (dto.Objectives is { Count: > 0 })
			{
				var list = dto.Objectives.Select(o => o.PositionIndex).ToList();
				if (list.Count != list.Distinct().Count())
					throw new ValidationException("Objective PositionIndex must be unique within the course.");
			}

			// Course Requirements
			if (dto.Requirements is { Count: > 0 })
			{
				var list = dto.Requirements.Select(r => r.PositionIndex).ToList();
				if (list.Count != list.Distinct().Count())
					throw new ValidationException("Requirement PositionIndex must be unique within the course.");
			}

			// Modules
			if (dto.Modules is { Count: > 0 })
			{
				var mIdx = dto.Modules.Select(m => m.PositionIndex).ToList();
				if (mIdx.Count != mIdx.Distinct().Count())
					throw new ValidationException("Module PositionIndex must be unique within the course.");

				foreach (var m in dto.Modules)
				{
					// Module Objectives
					if (m.Objectives is { Count: > 0 })
					{
						var list = m.Objectives.Select(o => o.PositionIndex).ToList();
						if (list.Count != list.Distinct().Count())
							throw new ValidationException($"Module '{m.ModuleName}' objectives' PositionIndex must be unique.");
					}
					// Lessons (nếu bạn muốn đảm bảo không trùng)
					if (m.Lessons is { Count: > 0 })
					{
						var list = m.Lessons.Select(l => l.PositionIndex).ToList();
						if (list.Count != list.Distinct().Count())
							throw new ValidationException($"Module '{m.ModuleName}' lessons' PositionIndex must be unique.");
					}
				}
			}
		}

		public async Task<GetCourseByIdResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
		{
			// Find(...) trả IQueryable<TEntity?> nên cast về IQueryable<CourseEntity> để dùng Include/ThenInclude
			var baseQuery = _courseRepository
				.Find(x => x.CourseId == id, isTracking: false, ct)
				.Cast<CourseEntity>()                              // quan trọng để dùng Include/ThenInclude
				.Include(x => x.Subject)
				.Include(x => x.CourseObjectives)
				.Include(x => x.CourseRequirements)
				.Include(x => x.Modules).ThenInclude(m => m.ModuleObjectives)
				.Include(x => x.Modules).ThenInclude(m => m.Lessons);

			var entity = await baseQuery.FirstOrDefaultAsync(ct);

			if (entity is null)
				return new GetCourseByIdResponse { Success = false, Message = $"Course {id} not found" };

			var detail = MapDetail(entity);
			var modulesCount = entity.Modules.Count(m => m.IsActive);                       // hoặc .Count(m => m.IsActive)
			var lessonsCount = entity.Modules.Sum(m => m.Lessons.Count(l => l.IsActive));   // hoặc .Sum(m => m.Lessons.Count(l => l.IsActive))

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
