namespace Course.Application.Interfaces.Helpers.Courses
{
	public interface ICourseCache
	{
		Task ClearGetAllCacheAsync();
		Task ClearCourseDetailForStudentCacheAsync();
		Task ClearCourseDetailForGuestCacheAsync();
		Task ClearCourseDetailForLectureCacheAsync();
		Task ClearEnrollmentStatusCacheAsync(Guid? userId = null, Guid? courseId = null);
		Task ClearCourseTagsCacheAsync();
	}
}
