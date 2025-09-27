using BuildingBlocks.Messaging.Events.CourseService.QuizCourseInsertEvents;
using Course.Application.DTOs.QuizDTO;

namespace Course.Infrastructure.Helpers.Courses
{
	public class QuizEventFactory : IQuizEventFactory
	{
		/// <summary>
		/// Convert CreateQuizDto to QuizCourseInsertEvent for publishing to message bus
		/// </summary>
		/// <param name="dto"></param>
		/// <returns></returns>
		public QuizCourseInsertEvent ToQuizCourseInsertEvent(string userEmail, CreateQuizDto q)
		{
			return new QuizCourseInsertEvent
			{
				UserEmail = userEmail,
				DurationMinutes = q.QuizSettings.DurationMinutes,
				PassingScorePercentage = q.QuizSettings.PassingScorePercentage,
				ShuffleQuestions = q.QuizSettings.ShuffleQuestions,
				ShowResultsImmediately = q.QuizSettings.ShowResultsImmediately,
				AllowRetake = q.QuizSettings.AllowRetake,
				Questions = q.Questions.Select(qq => new BuildingBlocks.Messaging.Events.CourseService.QuizCourseInsertEvents.Questions
				{
					QuestionText = qq.QuestionText,
					QuestionType = (short)qq.QuestionType, // 1 & 3 theo bạn yêu cầu
					Explanation = qq.Explanation,
					Answers = qq.Options.Select(a => new BuildingBlocks.Messaging.Events.CourseService.QuizCourseInsertEvents.Answers
					{
						AnswerText = a.Text,
						IsCorrect = a.IsCorrect
					}).ToList()
				}).ToList()
			};
		}
	}
}
