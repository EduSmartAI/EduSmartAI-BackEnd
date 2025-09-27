using Course.Application.DTOs.CoursesDTO;

namespace Course.Infrastructure.Helpers.Courses
{
	public sealed class CacheKeyFactory : ICacheKeyFactory
	{
		/// <summary>
		/// Generate cache key for GetAllAsync method based on pagination and query parameters
		/// </summary>
		/// <param name="pagination"></param>
		/// <param name="query"></param>
		/// <returns></returns>
		public string GenerateCacheKeyForGetAll(PaginationRequest pagination, CourseQuery? query)
		{
			var keyParts = new List<string>
		{
			"Courses:GetAll",
			$"PageIndex:{pagination.PageIndex}",
			$"PageSize:{pagination.PageSize}"
		};

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
	}
}
