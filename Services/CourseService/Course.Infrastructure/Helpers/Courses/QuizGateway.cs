using BuildingBlocks.Messaging.Events.CourseService.QuizCourseCheckAttemptEvents;
using BuildingBlocks.Messaging.Events.CourseService.QuizCourseSelectEvents;
using Course.Application.DTOs.QuizDTO;

namespace Course.Infrastructure.Helpers.Courses
{
	public sealed class QuizGateway(
		IRequestClient<QuizCourseSelectEvent> _quizSelectClient,
		IRequestClient<QuizCourseCheckAttemptEvent> _quizCourseCheckAttemptClient) : IQuizGateway
	{
		/// <summary>
		/// Fetch quiz details from Quiz Service for lecture
		/// </summary>
		/// <param name="quizId"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<QuizOutDto?> FetchQuizForLectureAsync(Guid quizId, CancellationToken ct)
		{
			var res = await _quizSelectClient.GetResponse<QuizCourseSelectEventResponse>(
				new QuizCourseSelectEvent { QuizId = quizId }, ct);

			if (!res.Message.Success || res.Message.Response is null)
				return null;

			return ToQuizOutForLectureDto(quizId, res.Message.Response);
		}

		/// <summary>
		/// Fetch quiz details from Quiz Service for student (without correct answers)
		/// </summary>
		/// <param name="quizId"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<QuizOutDto?> FetchQuizForStudentAsync(Guid quizId, CancellationToken ct)
		{
			var res = await _quizSelectClient.GetResponse<QuizCourseSelectEventResponse>(
				new QuizCourseSelectEvent { QuizId = quizId }, ct);

			if (!res.Message.Success || res.Message.Response is null)
				return null;

			return ToQuizOutForStudentDto(quizId, res.Message.Response);
		}

		/// <summary>
		/// Check if student can attempt the quiz
		/// </summary>
		/// <param name="quizId"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<QuizCourseCheckAttemptEventResponse> CheckCheckAttemptAsync(Guid quizId, Guid studentId, CancellationToken ct)
		{
			var response = new QuizCourseCheckAttemptEventResponse { Success = false };
			var res = await _quizCourseCheckAttemptClient.GetResponse<QuizCourseCheckAttemptEventResponse>(new QuizCourseCheckAttemptEvent(quizId, studentId), ct);

			if (!res.Message.Success || res.Message.Response is null)
			{
				response.SetMessage(MessageId.E99999, "Failed to check quiz attempt.");
				return response;
			}

			response.Success = true;
			response.Response = res.Message.Response;

			return response;
		}


		#region Helpers
		/// <summary>
		/// Map QuizCourseSelectEventResponseEntity to QuizOutDto
		/// </summary>
		/// <param name="quizId"></param>
		/// <param name="q"></param>
		/// <returns></returns>
		private static QuizOutDto ToQuizOutForLectureDto(Guid quizId, QuizCourseSelectEventResponseEntity q)
		{
			return new QuizOutDto(
				quizId,
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
					qq.Answers.Select(a => new QuizAnswerOutDto(a.AnswerId, a.AnswerText, a.IsCorrect)).ToList()
				)).ToList()
			);
		}

		private static QuizOutDto ToQuizOutForStudentDto(Guid quizId, QuizCourseSelectEventResponseEntity q)
		{
			return new QuizOutDto(
				quizId,
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
					qq.Answers.Select(a => new QuizAnswerOutDto(a.AnswerId, a.AnswerText, null)).ToList()
				)).ToList()
			);
		}
		#endregion
	}
}
