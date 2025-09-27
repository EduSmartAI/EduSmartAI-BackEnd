namespace Course.Application.Interfaces.Helpers
{
	public interface ICourseCache
	{
		Task ClearGetAllCacheAsync();
		Task ClearCourseDetailForStudentCacheAsync();
		Task ClearCourseTagsCacheAsync();
	}
}
