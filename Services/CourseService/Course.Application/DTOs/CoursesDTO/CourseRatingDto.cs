using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Course.Application.DTOs.CoursesDTO
{
	public record CourseRatingDto(
		Guid RatingId,
		Guid UserId,
		short Rating,
		DateTime CreatedAt
	);
}
