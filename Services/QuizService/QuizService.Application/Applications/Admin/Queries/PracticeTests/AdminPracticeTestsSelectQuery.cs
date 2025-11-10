using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.Admin.Queries.PracticeTests;

/// <summary>
/// Query to select all practice tests for admin with pagination and filters
/// </summary>
public record AdminPracticeTestsSelectQuery : IQuery<AdminPracticeTestsSelectResponse>
{
    public int PageNumber { get; init; } = 1;
    
    public int PageSize { get; init; } = 10;
    
    public string? Difficulty { get; init; }
    
    public string? SearchTitle { get; init; }
}

