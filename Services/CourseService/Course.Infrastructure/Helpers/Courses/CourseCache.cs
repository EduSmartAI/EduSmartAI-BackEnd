
namespace Course.Infrastructure.Helpers.Courses
{
	public sealed class CourseCache(IConnectionMultiplexer mux, IDatabase _cache) : ICourseCache
	{
		/// <summary>
		/// Clear cache for GetCourseDetailForGuest and GetCourseDetailBySlugForGuest
		/// </summary>
		public async Task ClearCourseDetailForGuestCacheAsync()
		{
			await DeleteByPatternAsync("CourseDetailForGuest*");
			await DeleteByPatternAsync("CourseDetailBySlugForGuest*");
		}

		/// <summary>
		/// Clear cache for GetCourseDetailForLecture and GetCourseDetailBySlugForLecture
		/// </summary>
		public async Task ClearCourseDetailForLectureCacheAsync()
		{
			await DeleteByPatternAsync("CourseDetailForLecture*");
			await DeleteByPatternAsync("CourseDetailBySlugForLecture*");
		}

		/// <summary>
		/// Clear cache for GetCourseByIdForStudentAsync and GetCourseBySlugForStudentAsync methods when user progress or enrollment changes
		/// </summary>
		/// <returns></returns>
		public async Task ClearCourseDetailForStudentCacheAsync() => await DeleteByPatternAsync("CourseDetailForStudent*");

		/// <summary>
		/// Clear cache for GetCourseTagsAsync method when tag data changes
		/// </summary>
		/// <returns></returns>
		public async Task ClearCourseTagsCacheAsync() => await DeleteByPatternAsync("CourseTags:*");

		/// <summary>
		/// Clear cache for enrollment status of all users or a specific course
		/// </summary>
		/// <param name="userId">Optional: specific user to clear</param>
		/// <param name="courseId">Optional: specific course to clear</param>
		public async Task ClearEnrollmentStatusCacheAsync(Guid? userId = null, Guid? courseId = null)
		{
			string pattern;

			if (userId.HasValue && courseId.HasValue)
			{
				pattern = $"enroll:status:{userId}:{courseId}";
			}
			else if (userId.HasValue)
			{
				pattern = $"enroll:status:{userId}:*";
			}
			else
			{
				pattern = "enroll:status:*";
			}

			await DeleteByPatternAsync(pattern);
		}

		/// <summary>
		/// Clear cache for GetAllAsync method when course data changes
		/// </summary>
		/// <returns></returns>
		public async Task ClearGetAllCacheAsync() => await DeleteByPatternAsync("Courses:GetAll*");

		private async Task DeleteByPatternAsync(string pattern)
		{
			var server = mux.GetServer(mux.GetEndPoints().FirstOrDefault()!);
			var keys = server.Keys(pattern: pattern);
			if (keys.Any()) await _cache.KeyDeleteAsync(keys.ToArray());
		}
	}
}
