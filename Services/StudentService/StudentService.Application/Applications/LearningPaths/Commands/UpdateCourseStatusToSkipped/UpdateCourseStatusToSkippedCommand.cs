using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.LearningPaths.Commands.UpdateCourseStatusToSkipped;

public class UpdateCourseStatusToSkippedCommand : ICommand<UpdateCourseStatusToSkippedResponse>
{
    [Required(ErrorMessage = "LearningPathCourseId is required")]
    public Guid CourseId { get; set; }
}

