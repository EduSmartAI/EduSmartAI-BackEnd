namespace Course.Application.UserLessonProgresses.Queries.GetMyLearningCourses
{
	public class GetMyLearningCoursesQueryHandler(IStudentProgressService _studentProgressService) : IQueryHandler<GetMyLearningCoursesQuery, GetMyLearningCoursesResponse>
	{
		public async Task<GetMyLearningCoursesResponse> Handle(GetMyLearningCoursesQuery request, CancellationToken cancellationToken)
		{
			return await _studentProgressService.GetMyLearningAsync(request, cancellationToken);
		}
	}
}
