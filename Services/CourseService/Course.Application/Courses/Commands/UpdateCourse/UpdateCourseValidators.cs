using Course.Application.DTOs;
using FluentValidation;

namespace Course.Application.Courses.Commands.UpdateCourse
{
	public class UpdateCourseValidators : AbstractValidator<UpdateCourseCommand>
	{
		public UpdateCourseValidators()
		{
			RuleFor(x => x.CourseId).NotEmpty();
			RuleFor(x => x.Payload.TeacherId).NotEmpty();
			RuleFor(x => x.Payload.SubjectId).NotEmpty();
			RuleFor(x => x.Payload.Title).NotEmpty().MaximumLength(200);
			RuleFor(x => x.Payload.Price).GreaterThan(0);
			RuleFor(x => x.Payload.DealPrice)
				.Must((cmd, deal) => deal is null || deal <= cmd.Payload.Price)
				.WithMessage("DealPrice must be <= Price.");

			RuleForEach(x => x.Payload.Modules).SetValidator(new UpdateModuleValidator());
			RuleFor(x => x.Payload.Modules.Select(m => m.PositionIndex))
				.Must(list => list.Distinct().Count() == list.Count())
				.WithMessage("Module PositionIndex must be unique within the course.");
		}
	}

	public class UpdateModuleValidator : AbstractValidator<UpdateModuleDto>
	{
		public UpdateModuleValidator()
		{
			RuleFor(x => x.ModuleName).NotEmpty().MaximumLength(200);
			RuleForEach(x => x.Lessons).SetValidator(new UpdateLessonValidator());
			RuleFor(x => x.Lessons.Select(l => l.PositionIndex))
				.Must(list => list.Distinct().Count() == list.Count())
				.WithMessage("Lesson PositionIndex must be unique within the module.");
		}
	}

	public class UpdateLessonValidator : AbstractValidator<UpdateLessonDto>
	{
		public UpdateLessonValidator()
		{
			RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
			RuleFor(x => x.VideoUrl).NotEmpty().MaximumLength(1000);
		}
	}
}
