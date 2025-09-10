using BaseService.Application.Interfaces.Repositories;
using BuildingBlocks.Pagination;
using Course.Application.Courses.Queries.GetCourses;
using Course.Application.DTOs;
using Course.Application.Interfaces;
using Course.Domain.Models;
using System.Linq.Expressions;

namespace Course.Infrastructure.Implements
{
	public class CourseService(ICommandRepository<CourseEntity> _courseRepository, IUnitOfWork unitOfWork) : ICourseService
	{
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
			catch(Exception ex)
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
