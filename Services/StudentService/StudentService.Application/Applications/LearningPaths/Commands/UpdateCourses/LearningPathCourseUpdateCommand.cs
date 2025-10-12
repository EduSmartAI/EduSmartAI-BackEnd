using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;
using StudentService.Application.Applications.LearningPaths.Commands.UpdateCourses;

namespace StudentService.Application.Applications.LearningPaths.Commands.UpdateCourses;

/// <summary>
/// Command to update selected courses in learning path
/// </summary>
public class LearningPathCourseUpdateCommand : ICommand<LearningPathCourseUpdateResponse>
{
    /// <summary>
    /// Learning Path ID
    /// </summary>
    [Required(ErrorMessage = "PathId is required")]
    public Guid PathId { get; set; }

    /// <summary>
    /// List of selected course IDs that user wants to keep active
    /// </summary>
    [Required(ErrorMessage = "SelectedCourseIds is required")]
    [MinLength(1, ErrorMessage = "At least one course must be selected")]
    public List<Guid> SelectedCourseIds { get; set; } = new();
}
