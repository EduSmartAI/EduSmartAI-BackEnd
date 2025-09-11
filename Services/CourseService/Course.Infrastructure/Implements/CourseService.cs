using BaseService.Application.Interfaces.Repositories;
using BuildingBlocks.Pagination;
using Course.Application.Courses.Queries.GetCourses;
using Course.Application.DTOs;
using Course.Application.Interfaces;
using Course.Domain.Models;
using Course.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Course.Infrastructure.Implements
{
	public class CourseService(ICourseRepository _courseRepository, IUnitOfWork unitOfWork, CourseDbContext _db) : ICourseService
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
						(x.Title != null && x.Title.Contains(search)) ||
						(x.Description != null && x.Description.Contains(search)));
				}

				if (query.SubjectId is Guid subjectId)
					pred = Acc(pred, x => x.SubjectId == subjectId);

				if (query.TeacherId is Guid teacherId)
					pred = Acc(pred, x => x.TeacherId == teacherId);

				if (query.IsActive is bool isActive)
					pred = Acc(pred, x => x.IsActive == isActive);

				predicate = pred;
			}

			// Chọn orderBy mặc định theo Title (tăng dần). Đổi nếu bạn muốn.
			Expression<Func<CourseEntity, string>> orderBy = x => x.Title;

			// Lưu ý: thường repository dùng pageNumber 1-based.
			// PaginationRequest đang 0-based (PageIndex). Chuyển sang 1-based để an toàn.
			var pageNumber = pagination.PageIndex + 1;
			var pageSize = pagination.PageSize;

			var page = await _courseRepository.PagedAsync(
				pageNumber: pageNumber,
				pageSize: pageSize,
				predicate: predicate,
				orderBy: orderBy,
				orderByDescending: false,
				cancellationToken: ct
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
		public async Task<CourseDetailDto> CreateAsync(CreateCourseDto dto, CancellationToken ct = default)
		{
			var now = DateTime.UtcNow;
			const string actor = "system"; // TODO: inject IUserContext để lấy username thực

			var course = new CourseEntity
			{
				CourseId = Guid.NewGuid(),
				TeacherId = dto.TeacherId,
				SubjectId = dto.SubjectId,
				Title = dto.Title,
				Description = dto.Description,
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

			// Map Modules + Lessons (giữ thứ tự PositionIndex)
			foreach (var m in dto.Modules.OrderBy(x => x.PositionIndex))
			{
				var module = new Module
				{
					ModuleId = Guid.NewGuid(),
					CourseId = course.CourseId,
					ModuleName = m.ModuleName,
					//Description = m.Description,
					PositionIndex = m.PositionIndex,
					IsActive = m.IsActive,
					CreatedAt = now,
					UpdatedAt = now,
					CreatedBy = actor,
					UpdatedBy = actor
				};

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
			try
			{
				await unitOfWork.BeginTransactionAsync(async () =>
							{
								await _courseRepository.AddAsync(course);   // hoặc Insert/Add tùy interface bạn đang dùng
								await unitOfWork.SaveChangesAsync(actor, ct);         // EF: SaveChanges; Marten: cũng qua UoW
																					  // Nếu có Outbox/Event:
																					  // _uow.Store(new CourseCreatedEvent { CourseId = course.CourseId, ... });
																					  // await _uow.SessionSaveChangesAsync();

								return true; // yêu cầu của BeginTransactionAsync: trả true để commit
							}, ct);
			}
			catch (Exception ex)
			{
				// Log lỗi nếu cần
				Console.WriteLine($"Error creating course: {ex.Message}");
				throw; // Rethrow để lỗi được xử lý ở tầng trên (middleware, handler, v.v.)
			}

			// Transaction qua UoW


			// Không bắt buộc reload: course hiện đã có đầy đủ nav (vì ta tự add vào graph).
			// Nếu bạn muốn AsNoTracking + Include để chắc chắn, có thể thêm QueryRepository để load lại.
			return MapDetail(course);
		}

		private static CourseDetailDto MapDetail(CourseEntity e)
		{
			var modules = e.Modules
				.OrderBy(m => m.PositionIndex)
				.Select(m => new ModuleDetailDto(
					m.ModuleId,
					m.ModuleName,
					//m.Description,
					m.PositionIndex,
					m.IsActive,
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
				e.Title,
				e.Description,
				e.DurationMinutes,
				e.Level,
				e.Price,
				e.DealPrice,
				e.IsActive,
				e.CreatedAt,
				e.UpdatedAt,
				modules
			);

		}




		private static CourseDto MapToDto(CourseEntity e) => new(
			CourseId: e.CourseId,
			TeacherId: e.TeacherId,
			SubjectId: e.SubjectId,
			Title: e.Title ?? string.Empty,
			Description: e.Description,
			DurationMinutes: e.DurationMinutes,
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
		public async Task<CourseDetailDto> UpdateAsync(Guid courseId, UpdateCourseDto dto, CancellationToken ct = default)
		{
			try
			{
				const string actor = "system";
				var now = DateTime.UtcNow;

				// 1) Load full graph (tracking) = Course + Modules + Lessons
				var course = await _courseRepository
					.Find(c => c.CourseId == courseId, isTracking: true, ct)
					.Include(c => c.Modules)
						.ThenInclude(m => m.Lessons)
					.FirstOrDefaultAsync(ct)
					?? throw new KeyNotFoundException($"Course {courseId} not found");

				// 2) Update root fields
				course.TeacherId = dto.TeacherId;
				course.SubjectId = dto.SubjectId;
				course.Title = dto.Title;
				course.Description = dto.Description;
				course.Price = dto.Price;
				course.DealPrice = dto.DealPrice;
				course.Level = dto.Level;
				course.DurationMinutes = dto.DurationMinutes;
				course.IsActive = dto.IsActive;
				course.UpdatedAt = now;
				course.UpdatedBy = actor;

				// Chuẩn hoá danh sách module/lesson DTO để thao tác
				var dtoModules = (dto.Modules ?? new List<UpdateModuleDto>()).OrderBy(m => m.PositionIndex).ToList();

				// Tập ID các module xuất hiện trong payload (chỉ tính những cái có ID cũ để làm "keep list")
				var keepModuleIds = new HashSet<Guid>(
					dtoModules.Where(m => m.ModuleId.HasValue)
							  .Select(m => m.ModuleId!.Value)
				);

				// 3) Xoá các Module KHÔNG còn trong payload (chỉ xoá những module cũ vắng mặt)
				var modulesToDelete = course.Modules
					.Where(m => !keepModuleIds.Contains(m.ModuleId))
					.ToList();
				foreach (var del in modulesToDelete)
				{
					// Cascade sẽ xoá luôn lessons nếu FK có ON DELETE CASCADE hoặc cấu hình relationship đúng
					course.Modules.Remove(del);
				}

				// 4) Upsert Modules (add/update) + Upsert Lessons bên trong
				foreach (var mDto in dtoModules)
				{
					if (mDto.ModuleId is null)
					{
						// 4.1) Thêm Module mới
						var newModule = new Module
						{
							//ModuleId = Guid.NewGuid(),
							CourseId = course.CourseId,
							ModuleName = mDto.ModuleName,
							//Description = mDto.Description,
							PositionIndex = mDto.PositionIndex,
							IsActive = mDto.IsActive,
							CreatedAt = now,
							UpdatedAt = now,
							CreatedBy = actor,
							UpdatedBy = actor
						};

						// Thêm Lessons mới (giữ PositionIndex)
						var dtoLessonsNew = (mDto.Lessons ?? new List<UpdateLessonDto>()).OrderBy(l => l.PositionIndex);
						foreach (var lDto in dtoLessonsNew)
						{
							var newLesson = new Lesson
							{
								//LessonId = Guid.NewGuid(),
								ModuleId = newModule.ModuleId,
								Title = lDto.Title,
								VideoUrl = lDto.VideoUrl,
								VideoDurationSec = lDto.VideoDurationSec,
								PositionIndex = lDto.PositionIndex,
								IsActive = lDto.IsActive,
								CreatedAt = now,
								UpdatedAt = now,
								CreatedBy = actor,
								UpdatedBy = actor
							};
							newModule.Lessons.Add(newLesson);
						}

						course.Modules.Add(newModule);
					}
					else
					{
						// 4.2) Cập nhật Module cũ
						var module = course.Modules.FirstOrDefault(x => x.ModuleId == mDto.ModuleId.Value)
							?? throw new KeyNotFoundException($"Module {mDto.ModuleId} not found in course {courseId}");

						module.ModuleName = mDto.ModuleName;
						//module.Description = mDto.Description;
						module.PositionIndex = mDto.PositionIndex;
						module.IsActive = mDto.IsActive;
						module.UpdatedAt = now;
						module.UpdatedBy = actor;

						// Upsert lessons
						var dtoLessons = (mDto.Lessons ?? new List<UpdateLessonDto>()).OrderBy(l => l.PositionIndex).ToList();

						// Keep list cho lessons cũ xuất hiện trong payload
						var keepLessonIds = new HashSet<Guid>(
							dtoLessons.Where(l => l.LessonId.HasValue)
									  .Select(l => l.LessonId!.Value)
						);

						// Xoá các lesson cũ không còn trong payload
						var lessonsToDelete = module.Lessons
							.Where(l => !keepLessonIds.Contains(l.LessonId))
							.ToList();
						foreach (var del in lessonsToDelete)
						{
							module.Lessons.Remove(del);
						}

						// Thêm/Cập nhật lessons
						foreach (var lDto in dtoLessons)
						{
							if (lDto.LessonId is null)
							{
								// Thêm mới
								var newLesson = new Lesson
								{
									//LessonId = Guid.NewGuid(),
									ModuleId = module.ModuleId,
									Title = lDto.Title,
									VideoUrl = lDto.VideoUrl,
									VideoDurationSec = lDto.VideoDurationSec,
									PositionIndex = lDto.PositionIndex,
									IsActive = lDto.IsActive,
									CreatedAt = now,
									UpdatedAt = now,
									CreatedBy = actor,
									UpdatedBy = actor
								};

								module.Lessons.Add(newLesson);
							}
							else
							{
								// Cập nhật
								var lesson = module.Lessons.FirstOrDefault(x => x.LessonId == lDto.LessonId.Value)
									?? throw new KeyNotFoundException($"Lesson {lDto.LessonId} not found in module {module.ModuleId}");

								lesson.Title = lDto.Title;
								lesson.VideoUrl = lDto.VideoUrl;
								lesson.VideoDurationSec = lDto.VideoDurationSec;
								lesson.PositionIndex = lDto.PositionIndex;
								lesson.IsActive = lDto.IsActive;
								lesson.UpdatedAt = now;
								lesson.UpdatedBy = actor;
							}
						}
					}
				}

				// 5) Lưu thay đổi trong Transaction của UoW
				await unitOfWork.BeginTransactionAsync(async () =>
				{
					//_courseRepository.Update(course);              // mark aggregate as modified
					await unitOfWork.SaveChangesAsync(actor, ct);  // persist
					return true;                                   // commit
				}, ct);

				// 6) Trả về DTO chi tiết (không reload vì graph đang tracking đầy đủ)
				return MapDetail(course);
			}
			catch (DbUpdateConcurrencyException ex)
			{
				foreach (var entry in ex.Entries)
				{
					Console.WriteLine($"Concurrency on {entry.Metadata.Name} state {entry.State}");
					Console.WriteLine($"Error mapping course detail: {ex.Message}");
				}
				throw;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error mapping course detail: {ex.Message}");
				throw;
			}
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
