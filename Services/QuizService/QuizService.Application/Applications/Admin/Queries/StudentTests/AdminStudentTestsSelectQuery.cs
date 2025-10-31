using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.Admin.Queries.StudentTests;

/// <summary>
/// Query to select all student tests for admin
/// </summary>
public record AdminStudentTestsSelectQuery : IQuery<AdminStudentTestsSelectResponse>
{
    public int PageNumber { get; init; } = 1;
    
    public int PageSize { get; init; } = 10;
    
    public Guid? StudentId { get; init; }
    
    public Guid? TestId { get; init; }
}

