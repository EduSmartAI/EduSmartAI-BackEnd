using BuildingBlocks.CQRS;
using Microsoft.AspNetCore.Http;

namespace StudentService.Application.Applications.Students.Commands.Inserts;

public class StudentTranscriptInsertCommand : ICommand<StudentTranscriptInsertResponse>
{
    public IFormFile TranscriptFile { get; set; }
}