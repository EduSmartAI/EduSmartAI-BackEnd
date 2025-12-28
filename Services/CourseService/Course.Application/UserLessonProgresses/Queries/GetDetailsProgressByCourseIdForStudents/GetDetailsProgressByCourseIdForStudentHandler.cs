namespace Course.Application.UserLessonProgresses.Queries.GetDetailsProgressByCourseIdForStudents
{
	public class GetDetailsProgressByCourseIdForStudentHandler(IStudentProgressService _studentProgressService) : IQueryHandler<GetDetailsProgressByCourseIdForStudentQuery, GetDetailsProgressByCourseIdForStudentResponse>
	{
		public async Task<GetDetailsProgressByCourseIdForStudentResponse> Handle(GetDetailsProgressByCourseIdForStudentQuery request, CancellationToken cancellationToken)
		{
			return await _studentProgressService.GetCourseByIdForStudentAsync(request.Id, cancellationToken);
		}
	}
}
