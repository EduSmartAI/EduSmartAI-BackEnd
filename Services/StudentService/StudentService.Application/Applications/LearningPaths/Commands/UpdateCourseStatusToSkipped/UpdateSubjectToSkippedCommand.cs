using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.LearningPaths.Commands.UpdateCourseStatusToSkipped;

public class UpdateSubjectToSkippedCommand : ICommand<UpdateSubjectToSkippedCommandResponse>
{
    [Required(ErrorMessage = "SubjectCode is required.")]
    public string SubjectCode { get; set;  }
}