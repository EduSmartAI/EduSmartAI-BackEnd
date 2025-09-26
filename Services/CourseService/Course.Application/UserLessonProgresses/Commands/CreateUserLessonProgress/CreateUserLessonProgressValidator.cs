using FluentValidation;

namespace Course.Application.UserLessonProgresses.Commands.CreateUserLessonProgress
{
	public class CreateUserLessonProgressValidator : AbstractValidator<CreateUserLessonProgressCommand>
	{
		public CreateUserLessonProgressValidator()
		{
			RuleFor(x => x.UserLessonProgress).NotNull();
			RuleFor(x => x.UserLessonProgress.LessonId).NotEmpty();
		}
	}
}
