using BaseService.Application.Interfaces.Repositories;
using BuildingBlocks.Pagination;
using Course.Application.Courses.Queries.GetCourses;
using Course.Application.DTOs;
using Course.Application.Interfaces;
using System.Linq.Expressions;

namespace Course.Infrastructure.Implements
{
	public class CourseService(ICommandRepository<CourseEntity> _courseRepository) : ICourseService
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
