using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;

namespace StudentService.Application.Students.Commands.Inserts;

public record StudentInsertProfileCommand : ICommand<StudentInsertProfileResponse>
{
    [Required(ErrorMessage = "MajorId is required")]
    public Guid MajorId { get; set; }
    
    [Required(ErrorMessage = "SemesterId is required")]
    public int SemesterId { get; set; }
}
