using FluentValidation;

namespace AiService.Application.Handler.Quizzes.Commands
{
	public class QuizEvaluateValidator : AbstractValidator<QuizEvaluateCommand>
	{
		public QuizEvaluateValidator()
		{
			RuleFor(x => x.QuizEvaluableCreatedEvent).NotNull().WithMessage("QuizEvaluableCreatedEvent must not be null");
			RuleFor(x => x.QuizEvaluableCreatedEvent.TotalQuestions).GreaterThan(0).WithMessage("TotalQuestions must be greater than 0");
		}
	}
}
