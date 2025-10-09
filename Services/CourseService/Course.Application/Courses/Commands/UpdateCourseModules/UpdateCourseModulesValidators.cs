using Course.Application.DTOs.CoursesDTO;

namespace Course.Application.Courses.Commands.UpdateCourseModules
{
	public class UpdateCourseModulesValidators : AbstractValidator<UpdateCourseModulesCommand>
	{
		public UpdateCourseModulesValidators()
		{
			RuleFor(x => x.CourseId).NotEmpty();
			RuleFor(x => x.UpdateCourseModules.Modules).NotEmpty().WithMessage("At least one module is required.");

			// Validate each module
			RuleForEach(x => x.UpdateCourseModules.Modules).SetValidator(new UpdateCourseModuleValidator());
		}
	}

	public class UpdateCourseModuleValidator : AbstractValidator<UpdateCourseModuleDto>
	{
		public UpdateCourseModuleValidator()
		{
			RuleFor(x => x.ModuleName).NotEmpty().MaximumLength(150);
			RuleFor(x => x.Description).MaximumLength(500);
			RuleFor(x => x.PositionIndex).GreaterThan(0);
			RuleFor(x => x.DurationMinutes).GreaterThan(0).When(x => x.DurationMinutes.HasValue);
			//RuleFor(x => x.Level).GreaterThan(0).When(x => x.Level.HasValue);

			// Validate ModuleObjectives
			RuleForEach(x => x.Objectives).SetValidator(new UpdateCourseModuleObjectiveValidator());

			// Validate Lessons
			RuleForEach(x => x.Lessons).SetValidator(new UpdateCourseLessonValidator());
		}
	}

	public class UpdateCourseModuleObjectiveValidator : AbstractValidator<UpdateCourseModuleObjectiveDto>
	{
		public UpdateCourseModuleObjectiveValidator()
		{
			RuleFor(x => x.Content).NotEmpty();
			RuleFor(x => x.PositionIndex).GreaterThan(0);
		}
	}

	public class UpdateCourseLessonValidator : AbstractValidator<UpdateCourseLessonDto>
	{
		public UpdateCourseLessonValidator()
		{
			RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
			RuleFor(x => x.VideoUrl).NotEmpty().MaximumLength(1000);
			RuleFor(x => x.PositionIndex).GreaterThan(0);
			RuleFor(x => x.VideoDurationSec).GreaterThan(0).When(x => x.VideoDurationSec.HasValue);
		}
	}
}
