using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;
using Microsoft.EntityFrameworkCore;
using QuizService.Domain.WriteModels;

namespace QuizService.Application.Applications.Admin.Queries.PracticeTests;

public class AdminPracticeTestSelectQueryHandler : IQueryHandler<AdminPracticeTestSelectQuery, AdminPracticeTestSelectResponse>
{
    private readonly ICommandRepository<Problem> _problemRepository;
    private readonly ICommandRepository<CodeLanguage> _codeLanguageRepository;

    public AdminPracticeTestSelectQueryHandler(
        ICommandRepository<Problem> problemRepository,
        ICommandRepository<CodeLanguage> codeLanguageRepository)
    {
        _problemRepository = problemRepository;
        _codeLanguageRepository = codeLanguageRepository;
    }

    public async Task<AdminPracticeTestSelectResponse> Handle(AdminPracticeTestSelectQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminPracticeTestSelectResponse { Success = false };

        // Select problem with all related data
        var problem = await _problemRepository
            .Find(
                predicate: x => x.ProblemId == request.ProblemId && x.IsActive,
                isTracking: false,
                cancellationToken: cancellationToken,
                include: q => q
                    .Include(x => x.ProblemExamples)
                    .Include(x => x.TestCases)
                    .Include(x => x.ProblemTemplates)
                    .Include(x => x.ProblemSolutions)
                    .ThenInclude(s => s.Language)
            )
            .FirstOrDefaultAsync(cancellationToken);
        if (problem == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy bài kiểm tra thực hành");
            return response;
        }

        // Get all code languages for mapping
        var languages = await _codeLanguageRepository
            .Find(predicate: x => x.IsActive, isTracking: false)
            .ToDictionaryAsync(x => x.LanguageId, x => x.Name, cancellationToken);

        // Map to response
        var responseEntity = new AdminPracticeTestSelectResponseEntity
        {
            ProblemId = problem.ProblemId,
            Title = problem.Title,
            Description = problem.Description,
            Difficulty = problem.Difficulty,
            CreatedAt = problem.CreatedAt,
            Examples = problem
                .ProblemExamples
                .Where(x => x.IsActive)
                .Select(e => new AdminPracticeTestExample
                {
                    ExampleId = e.ExampleId,
                    ExampleOrder = e.ExampleOrder,
                    InputData = e.InputData,
                    OutputData = e.OutputData,
                    Explanation = e.Explanation,
                })
                .OrderBy(e => e.ExampleOrder)
                .ToList(),
            TestCases = problem
                .TestCases
                .Where(x => x.IsActive)
                .Select(tc => new AdminPracticeTestTestCase
                {
                    TestcaseId = tc.TestcaseId,
                    InputData = tc.InputData,
                    ExpectedOutput = tc.ExpectedOutput,
                    IsPublic = tc.IsPublic ?? false,
                })
                .ToList(),
            Templates = problem
                .ProblemTemplates
                .Where(x => x.IsActive)
                .Select(t => new AdminPracticeTestTemplate
                {
                    TemplateId = t.TemplateId,
                    LanguageId = t.LanguageId,
                    LanguageName = languages.GetValueOrDefault(t.LanguageId, "Unknown"),
                    TemplatePrefix = t.TemplatePrefix,
                    TemplateSuffix = t.TemplateSuffix,
                    UserStubCode = t.UserStubCode,
                })
                .ToList(),
            Solutions = problem
                .ProblemSolutions
                .Where(x => x.IsActive)
                .Select(s => new AdminPracticeSolution
                {
                    SolutionId = s.SolutionId,
                    SolutionCode = s.SolutionCode,
                    Language = new AdminPracticeSolution.LanguageInfo
                    {
                        LanguageId = s.Language.LanguageId,
                        LanguageName = s.Language.Name
                    }
                }).ToList()
        };

        // True
        response.Success = true;
        response.Response = responseEntity;
        response.SetMessage(MessageId.I00001, "Lấy thông tin bài kiểm tra thực hành");

        return response;
    }
}

