namespace Course.Application.Courses.Commands.RatingCourse
{
	public class UpsertCourseRatingHandler(IStudentProgressService _studentProgressService) : ICommandHandler<UpsertCourseRatingCommand, UpsertCourseRatingResponse>
	{
		public async Task<UpsertCourseRatingResponse> Handle(UpsertCourseRatingCommand request, CancellationToken cancellationToken)
		{
			return await _studentProgressService.UpsertCourseRatingAsync(request.CourseId, request.Rating, cancellationToken);
		}
	}
}
