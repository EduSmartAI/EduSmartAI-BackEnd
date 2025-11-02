using StudentService.Domain.ReadModels;

namespace StudentService.Application.Applications.Students.Consumers;

public class StudentCollectionEvent
{
    public StudentCollection Student { get; set; }
}