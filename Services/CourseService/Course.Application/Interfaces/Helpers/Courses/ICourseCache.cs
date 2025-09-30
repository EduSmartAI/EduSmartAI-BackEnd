namespace Course.Application.Interfaces.Helpers.Courses
{
	public interface ICourseCache
	{
		Task ClearGetAllCacheAsync();
		Task ClearCourseDetailForStudentCacheAsync();
		Task ClearCourseTagsCacheAsync();
	}
}
