using BuildingBlocks.Messaging.Events.CourseService.QuizCourseInsertEvents;
using Course.Application.DTOs.QuizDTO;

namespace Course.Application.Interfaces.Helpers
{
	public interface IQuizEventFactory
	{
		QuizCourseInsertEvent ToQuizCourseInsertEvent(string userEmail, CreateQuizDto q);
	}
}
