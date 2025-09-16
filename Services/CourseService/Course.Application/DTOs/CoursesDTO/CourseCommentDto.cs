using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Course.Application.DTOs.CoursesDTO
{
	public record CourseCommentDto(
		Guid CommentId,
		Guid UserId,
		string Content,
		Guid? ParentCommentId,
		DateTime CreatedAt,
		bool IsActive
	);
}
