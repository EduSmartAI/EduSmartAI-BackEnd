using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Course.Application.Courses.Queries.GetMyCourseRating
{
	public class GetMyCourseRatingHandler(IStudentProgressService studentProgressService) : IQueryHandler<GetMyCourseRatingQuery, GetMyCourseRatingResponse>
	{
		public async Task<GetMyCourseRatingResponse> Handle(GetMyCourseRatingQuery request, CancellationToken cancellationToken)
		{
			return await studentProgressService.IsCourseRatedByCurrentUserAsync(request.CourseId, cancellationToken);
		}
	}
}
