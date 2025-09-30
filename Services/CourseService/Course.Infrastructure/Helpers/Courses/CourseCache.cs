namespace Course.Infrastructure.Helpers.Courses
{
	public sealed class CourseCache(IConnectionMultiplexer mux, IDatabase _cache) : ICourseCache
	{
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
