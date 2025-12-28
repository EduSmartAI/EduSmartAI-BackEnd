namespace Course.Application.UserLessonProgresses.Queries.CheckEnrollment
{
	public class CheckEnrollmentHandler(IStudentProgressService _studentProgressService) : IQueryHandler<CheckEnrollmentQuery, CheckEnrollmentResponse>
	{
		public async Task<CheckEnrollmentResponse> Handle(CheckEnrollmentQuery request, CancellationToken cancellationToken)
		{
			return await _studentProgressService.CheckEnrollmentAsync(request.CourseId, cancellationToken);
		}
	}
}