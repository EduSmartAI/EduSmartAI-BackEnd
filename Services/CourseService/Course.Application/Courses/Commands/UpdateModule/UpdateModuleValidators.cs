using Course.Application.DTOs.LessonsDTO;
using FluentValidation;

namespace Course.Application.Courses.Commands.UpdateModule
{
	public class UpdateModuleValidators : AbstractValidator<UpdateModuleCommand>
	{
		public UpdateModuleValidators()
		{
			RuleFor(x => x.ModuleId).NotEmpty();
			RuleFor(x => x.UpdateModuleDto.ModuleName).NotEmpty().MaximumLength(150);
			RuleFor(x => x.UpdateModuleDto.Description).MaximumLength(500);
			RuleFor(x => x.UpdateModuleDto.PositionIndex).GreaterThan(0);
			RuleFor(x => x.UpdateModuleDto.DurationMinutes).GreaterThan(0).When(x => x.UpdateModuleDto.DurationMinutes.HasValue);
			//RuleFor(x => x.UpdateModuleDto.Level).GreaterThan(0).When(x => x.UpdateModuleDto.Level.HasValue);

			// Validate ModuleObjectives
			RuleForEach(x => x.UpdateModuleDto.Objectives).SetValidator(new UpdateModuleObjectiveValidator());

			// Validate Lessons
			RuleForEach(x => x.UpdateModuleDto.Lessons).SetValidator(new UpdateLessonValidator());
		}
	}

	public class UpdateModuleObjectiveValidator : AbstractValidator<UpdateModuleObjectiveDto>
	{
		public UpdateModuleObjectiveValidator()
		{
			RuleFor(x => x.Content).NotEmpty();
			RuleFor(x => x.PositionIndex).GreaterThan(0);
		}
	}

	public class UpdateLessonValidator : AbstractValidator<UpdateLessonDto>
	{
		public UpdateLessonValidator()
		{
			RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
			RuleFor(x => x.VideoUrl).NotEmpty().MaximumLength(1000);
			RuleFor(x => x.PositionIndex).GreaterThan(0);
			RuleFor(x => x.VideoDurationSec).GreaterThan(0).When(x => x.VideoDurationSec.HasValue);
		}
	}
}
