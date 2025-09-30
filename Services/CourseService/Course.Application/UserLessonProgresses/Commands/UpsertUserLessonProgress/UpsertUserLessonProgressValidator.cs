using FluentValidation;

namespace Course.Application.UserLessonProgresses.Commands.UpsertUserLessonProgress
{
	public class UpsertUserLessonProgressValidator : AbstractValidator<UpsertUserLessonProgressCommand>
	{
		public UpsertUserLessonProgressValidator()
		{
			RuleFor(x => x.LessonId).NotEmpty();
		}
	}
}
