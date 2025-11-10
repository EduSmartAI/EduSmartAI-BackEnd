using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;
using Microsoft.EntityFrameworkCore;
using QuizService.Domain.WriteModels;

namespace QuizService.Application.Applications.Admin.Queries.PracticeTests;

public class AdminPracticeTestsSelectQueryHandler : IQueryHandler<AdminPracticeTestsSelectQuery, AdminPracticeTestsSelectResponse>
{
    private readonly ICommandRepository<Problem> _problemRepository;
    private readonly ICommandRepository<Submission> _submissionRepository;

    public AdminPracticeTestsSelectQueryHandler(
        ICommandRepository<Problem> problemRepository,
        ICommandRepository<Submission> submissionRepository)
    {
        _problemRepository = problemRepository;
        _submissionRepository = submissionRepository;
    }

    public async Task<AdminPracticeTestsSelectResponse> Handle(AdminPracticeTestsSelectQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminPracticeTestsSelectResponse { Success = false };

        // Build query
        var query = _problemRepository.Find(
            predicate: x => x.IsActive,
            isTracking: false,
            cancellationToken: cancellationToken,
            x => x.TestCases,
            x => x.ProblemExamples,
            x => x.ProblemTemplates,
            x => x.Submissions);

        // Filter by difficulty
        if (!string.IsNullOrEmpty(request.Difficulty))
        {
            query = query.Where(p => p.Difficulty == request.Difficulty);
        }

        // Filter by title search
        if (!string.IsNullOrEmpty(request.SearchTitle))
        {
            query = query.Where(p => p.Title.Contains(request.SearchTitle));
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var problems = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        if (!problems.Any())
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy bài kiểm tra thực hành nào");
            return response;
        }

        // Map to response
        var practiceTestItems = problems.Select(p => new AdminPracticeTestItem
        {
            ProblemId = p.ProblemId,
            Title = p.Title,
            Description = p.Description,
            Difficulty = p.Difficulty,
            TotalTestCases = p.TestCases.Count(tc => tc.IsActive),
            TotalExamples = p.ProblemExamples.Count(e => e.IsActive),
            TotalTemplates = p.ProblemTemplates.Count(t => t.IsActive),
            TotalSubmissions = p.Submissions.Count(s => s.IsActive),
            CreatedAt = p.CreatedAt
        }).ToList();

        response.Success = true;
        response.Response = new AdminPracticeTestsSelectResponseEntity
        {
            PracticeTests = practiceTestItems,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
        response.SetMessage(MessageId.I00001, "Lấy danh sách bài kiểm tra thực hành");

        return response;
    }
}

