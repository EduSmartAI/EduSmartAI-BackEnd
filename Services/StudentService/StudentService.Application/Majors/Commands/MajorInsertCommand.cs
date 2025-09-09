using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;

namespace StudentService.Application.Majors.Commands;

public record MajorInsertCommand : ICommand<MajorInsertResponse>
{
    [Required(ErrorMessage = "MajorName is required")]
    public string MajorName { get; init;  } = null!;
    
    [Required(ErrorMessage = "MajorCode is required")]
    public string MarjorCode { get; init; } = null!;
    
    public string? Description { get; init; }
}