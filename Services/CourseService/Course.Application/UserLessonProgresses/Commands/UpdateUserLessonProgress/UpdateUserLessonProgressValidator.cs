using FluentValidation;

namespace Course.Application.UserLessonProgresses.Commands.UpdateUserLessonProgress
{
	public class UpdateUserLessonProgressValidator : AbstractValidator<UpdateUserLessonProgressCommand>
	{
		public UpdateUserLessonProgressValidator()
		{
			RuleFor(x => x.UpdateUserLessonProgress).NotNull();
			RuleFor(x => x.UpdateUserLessonProgress.LessonId).NotEmpty();
		}
	}
}
