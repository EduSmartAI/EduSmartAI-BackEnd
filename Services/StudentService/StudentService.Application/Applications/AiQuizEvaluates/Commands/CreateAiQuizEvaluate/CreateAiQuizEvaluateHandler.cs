using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.AiQuizEvaluates.Commands.CreateAiQuizEvaluate
{
	public class CreateAiQuizEvaluateHandler(IAiQuizEvaluateStudentService _aiQuizEvaluateService) : ICommandHandler<CreateAiQuizEvaluateCommand, CreateAiQuizEvaluateResponse>
	{
		public async Task<CreateAiQuizEvaluateResponse> Handle(CreateAiQuizEvaluateCommand request, CancellationToken cancellationToken)
		{
			return await _aiQuizEvaluateService.CreateAiQuizEvaluate(request.AiEvaluationUpsertEvent, cancellationToken);
		}
	}
}
