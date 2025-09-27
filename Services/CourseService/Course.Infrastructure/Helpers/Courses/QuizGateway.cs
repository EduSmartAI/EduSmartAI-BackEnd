using BuildingBlocks.Messaging.Events.CourseService.QuizCourseSelectEvents;
using Course.Application.DTOs.QuizDTO;
using Course.Application.Interfaces.Helpers;
using MassTransit;

namespace Course.Infrastructure.Helpers.Courses
{
	public sealed class QuizGateway(IRequestClient<QuizCourseSelectEvent> _quizSelectClient) : IQuizGateway
	{
		/// <summary>
		/// Fetch quiz details from Quiz Service
		/// </summary>
		/// <param name="quizId"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<QuizOutDto?> FetchQuizAsync(Guid quizId, CancellationToken ct)
		{
			var res = await _quizSelectClient.GetResponse<QuizCourseSelectEventResponse>(
				new QuizCourseSelectEvent { QuizId = quizId }, ct);

			if (!res.Message.Success || res.Message.Response is null)
				return null;

			return ToQuizOutDto(res.Message.Response);
		}

		private static QuizOutDto ToQuizOutDto(QuizCourseSelectEventResponseEntity q)
		{
			return new QuizOutDto(
				new QuizSettingsOutDto(
					q.DurationMinutes,
					q.PassingScorePercentage,
					q.ShuffleQuestions,
					q.ShowResultsImmediately,
					q.AllowRetake
				),
				q.Questions.Select(qq => new QuizQuestionOutDto(
					qq.QuestionId,
					qq.QuestionText,
					qq.Explanation,
					qq.QuestionType,
					qq.Answers.Select(a => new QuizAnswerOutDto(a.AnswerId, a.AnswerText)).ToList()
				)).ToList()
			);
		}
	}
}
