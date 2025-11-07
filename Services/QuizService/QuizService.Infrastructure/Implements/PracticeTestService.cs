using BaseService.Application.Common;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using Microsoft.EntityFrameworkCore;
using QuizService.Application.Applications.PracticeTest;
using QuizService.Application.Interfaces;
using QuizService.Application.Judge0Logics;
using QuizService.Application.Judge0Logics.Models;
using QuizService.Domain.WriteModels;
using StackExchange.Redis;

namespace QuizService.Infrastructure.Implements;

public class PracticeTestService
    (ICommandRepository<Problem> problemCommandRepository, 
        IDatabase cache,
        ICommandRepository<CodeLanguage> codeLanguageRepository,
        ICommandRepository<Submission> submissionRepository,
        IIdentityService identityService,
        IUnitOfWork unitOfWork,
        IJudge0ApiLogic judge0ApiLogic) 
    : IPracticeTestService
{
    /// <summary>
    /// Select Practice Test
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<PracticeTestSelectResponse> SelectPracticeTestAsync(PracticeTestSelectRequest request, CancellationToken cancellationToken)
    {
        var response = new PracticeTestSelectResponse { Success = false };
        
        var cacheKey = CacheKey.PracticeTestSelect(request.ProblemId);

        // Try get from cache
        var cached = await cache.StringGetAsync(cacheKey);
        if (cached.HasValue)
        {
            var cachedData = System.Text.Json.JsonSerializer.Deserialize<PracticeTestSelectResponseEntity>(cached!);
            response.Success = true;
            response.Response = cachedData!;
            response.SetMessage(MessageId.I00001, "Lấy đề kiểm tra thực hành");
            return response;
        }

        // Select problem
        var problem = await problemCommandRepository
            .Find(predicate:x => x.ProblemId == request.ProblemId && x.IsActive,
                isTracking: false,
                  cancellationToken: cancellationToken,
                  x => x.ProblemExamples,
                x => x.TestCases)
            .Select(x => new PracticeTestSelectResponseEntity
            {
                ProblemId = x.ProblemId,
                Title = x.Title,
                Description = x.Description,
                Difficulty = x.Difficulty,
                Examples = x.ProblemExamples
                    .Where(e => e.IsActive)
                    .OrderBy(e => e.ExampleOrder)
                    .Select(e => new PracticeTestProblemExample
                    {
                        ExampleId = e.ExampleId,
                        ExampleOrder = e.ExampleOrder,
                        InputData = e.InputData,
                        OutputData = e.OutputData
                    })
                    .ToList(),
                TestCases = x.TestCases
                    .Where(ts => ts.IsActive && ts.IsPublic == true)
                    .Select(ts => new PracticeTestSelectTestCaseResponse
                    {
                        ProblemId = x.ProblemId,
                        TestcaseId = ts.TestcaseId,
                        ExpectedOutput = ts.ExpectedOutput,
                        InputData = ts.InputData
                    }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (problem == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy đề kiểm tra thực hành");
            return response;
        }

        // Save to cache
        var json = System.Text.Json.JsonSerializer.Serialize(problem);
        await cache.StringSetAsync(cacheKey, json, TimeSpan.FromMinutes(10));

        // True
        response.Success = true;
        response.Response = problem;
        response.SetMessage(MessageId.I00001, "Lấy đề kiểm tra thực hành");
        return response;
    }

    /// <summary>
    /// Select Practice Tests
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<PracticeTestSelectsResponse> SelectPracticeTestsAsync(PracticeTestSelectsRequest request, CancellationToken cancellationToken)
    {
        var response = new PracticeTestSelectsResponse { Success = false };

        // Select problems from DB
        var pagedProblems = await problemCommandRepository.PagedAsync<int>(
            pageNumber: request.PageNumber,
            pageSize: request.PageSize,
            predicate: x => x.IsActive,
            orderBy: null,
            orderByDescending: false,
            cancellationToken: cancellationToken,
            includes: x => x.ProblemExamples);

        var mapped = new PagedResult<PracticeTestSelectsResponseEntity>
        {
            Items = pagedProblems.Items.Select(x => new PracticeTestSelectsResponseEntity
            {
                ProblemId = x.ProblemId,
                Title = x.Title,
                Description = x.Description,
                Difficulty = x.Difficulty
            }).ToList(),
            TotalCount = pagedProblems.TotalCount,
            PageNumber = pagedProblems.PageNumber,
            PageSize = pagedProblems.PageSize
        };
        
        // True
        response.Success = true;
        response.Response = mapped;
        response.SetMessage(MessageId.I00001, "Lấy danh sách các bài kiểm tra thực hành");
        return response;
    }

    public async Task<PracticeTestLanguageSelectsResponse> SelectPracticeTestLanguagesAsync(PracticeTestLanguageSelectsRequest request, CancellationToken cancellationToken)
    {
        var response = new PracticeTestLanguageSelectsResponse { Success = false };
        
        // Select code languages
        var languages = await codeLanguageRepository
            .Find(predicate: x => x.IsActive, isTracking: false)
            .Select(x => new PracticeTestLanguageSelectsResponseEntity
            {
                LanguageId = x.LanguageId,
                Name = x.Name
            })
            .ToListAsync(cancellationToken: cancellationToken);
        if (!languages.Any())
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy ngôn ngữ lập trình");
            return response;
        }
        
        // True
        response.Success = true;
        response.Response = languages;
        response.SetMessage(MessageId.I00001, "Lấy danh sách ngôn ngữ lập trình");
        return response;
    }

    public async Task<PracticeTestSubmitInsertResponse> InsertPracticeTestSubmitAsync(PracticeTestSubmitInsertRequest request, CancellationToken cancellationToken)
    {
        var response = new PracticeTestSubmitInsertResponse { Success = false };

        var currentUser = identityService.GetCurrentUser()!;
        
        // Check problem exists
        var problem = await problemCommandRepository
            .Find(predicate: x => x.ProblemId == request.ProblemId && x.IsActive,
                isTracking: true,
                cancellationToken: cancellationToken,
                includes: x => x.TestCases)
            .FirstOrDefaultAsync(cancellationToken);
        if (problem == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy đề kiểm tra thực hành");
            return response;
        }
        
        // Begin transaction
        await unitOfWork.BeginTransactionAsync(async () =>
        {
            // Create submission
            var submission = new Submission
            {
                SubmissionId = Guid.NewGuid(),
                ProblemId = request.ProblemId,
                StudentId = currentUser.UserId,
                Code = request.SourceCode,
                LanguageId = request.LanguageId,
            };
            
            // Submit to Judge0 API
            var batchRequest = new BatchSubmissionRequest
            {
                Submissions = problem.TestCases.Where(ts => ts.IsPublic == false).Select(tc => new SubmissionRequest
                {
                    SourceCode = request.SourceCode,
                    LanguageId = request.LanguageId,
                    Stdin = tc.InputData,
                    ExpectedOutput = tc.ExpectedOutput,
                    CpuTimeLimit = 2.0,
                    MemoryLimit = 128000
                }).ToList()
            };
            
            // Call Judge0 API to submit batch
            var submissResult = await judge0ApiLogic.SubmitBatchAsync(batchRequest);
            var tokens = string.Join(",", submissResult.Submissions.Select(s => s.Token));
            
            List<SubmissionResult>? pollResults = null;
            int maxRetries = 10;
            int retryCount = 0;
            
            do
            {
                await Task.Delay(1000, cancellationToken);
                var batchResult = await judge0ApiLogic.GetBatchSubmissionAsync(tokens);
            
                // Check all results are completed
                bool allCompleted = batchResult.All(s => s.Status.Id > (short) ConstantEnum.Judge0Status.Processing);
                if (allCompleted)
                {
                    pollResults = batchResult;
                    break;
                }
            
                retryCount++;
            }
            while (retryCount < maxRetries);

            if (retryCount >= maxRetries)
            {
                submission.Status = nameof(ConstantEnum.PracticeTestSubmissionStatus.TimeOut);
            }
            
            // Processing results
            int passedCount = 0;
            long totalTimeMs = 0;
            var testResults = new List<SubmissionTestResultResponse>();

            for (int i = 0; i < pollResults.Count; i++)
            {
                var result = pollResults[i];
                var testCase = problem.TestCases.ToList()[i];

                bool passed = result.Status.Id == (short) ConstantEnum.Judge0Status.Accepted;
                if (passed) passedCount++;

                totalTimeMs += (long)((result.Time ?? 0) * 1000);

                // Insert SubmissionTestResult
                var testResult = new SubmissionTestResult
                {
                    SubmissionTestId = Guid.NewGuid(),
                    SubmissionId = submission.SubmissionId,
                    TestcaseId = testCase.TestcaseId,
                    Passed = passed,
                    ActualOutput = result.Stdout?.Trim() ?? result.Stderr,
                };
                submission.SubmissionTestResults.Add(testResult);
                
                await submissionRepository.AddAsync(submission);

                testResults.Add(new SubmissionTestResultResponse
                {
                    TestCaseId = testCase.TestcaseId,
                    IsPublic = testCase.IsPublic ?? false,
                    InputData = (testCase.IsPublic ?? false) ? testCase.InputData : "Hidden",
                    ExpectedOutput = (testCase.IsPublic ?? false) ? testCase.ExpectedOutput : "Hidden",
                    ActualOutput = testResult.ActualOutput,
                    Passed = passed,
                    Status = result.Status.Description,
                });
            }

            // Update submission status to Pass
            submission.Status = nameof(ConstantEnum.PracticeTestSubmissionStatus.Pass);
            await unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken);
            
            // True
            response.Success = true;
            response.Response = new PracticeTestSubmitInsertResponseEntity
            {
                SubmissionId = submission.SubmissionId,
                Status = DetermineStatus(pollResults),
                PassedTests = passedCount,
                TotalTests = problem.TestCases.Count,
                AverageTimeMs = pollResults.Count > 0 ? (int) (totalTimeMs / pollResults.Count) : 0,
                TestResults = testResults
            };
            response.SetMessage(MessageId.I00001, "Nộp bài kiểm tra thực hành");
            return true;
        }, cancellationToken);
        return response;
    }
    
    private string DetermineStatus(List<SubmissionResult> results)
    {
        if (results.All(r => r.Status.Id == (short) ConstantEnum.Judge0Status.Accepted))
            return "Accepted";

        if (results.Any(r => r.Status.Id == (short) ConstantEnum.Judge0Status.CompilationError))
            return "Compilation Error";

        if (results.Any(r => r.Status.Id == (short) ConstantEnum.Judge0Status.TimeLimitExceeded))
            return "Time Limit Exceeded";

        if (results.Any(r => r.Status.Id >= (short) ConstantEnum.Judge0Status.RuntimeErrorSIGSEGV && 
                             r.Status.Id <= (short) ConstantEnum.Judge0Status.RuntimeErrorOther))
            return "Runtime Error";

        if (results.Any(r => r.Status.Id == (short) ConstantEnum.Judge0Status.WrongAnswer))
            return "Wrong Answer";

        return "Failed";
    }
}