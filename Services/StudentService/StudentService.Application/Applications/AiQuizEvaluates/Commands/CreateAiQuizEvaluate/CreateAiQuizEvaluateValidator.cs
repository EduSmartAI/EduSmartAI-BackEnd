using FluentValidation;

namespace StudentService.Application.Applications.AiQuizEvaluates.Commands.CreateAiQuizEvaluate
{
	public class CreateAiQuizEvaluateValidator : AbstractValidator<CreateAiQuizEvaluateCommand>
	{
		public CreateAiQuizEvaluateValidator()
		{
			RuleFor(x => x.AiEvaluationUpsertEvent).NotEmpty().NotNull().WithMessage("AiEvaluationUpsertEvent model must not be null");
		}
	}
}
