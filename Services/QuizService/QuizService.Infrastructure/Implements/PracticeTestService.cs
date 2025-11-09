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
        ICommandRepository<ProblemTemplate> problemTemplateRepository,
        ICommandRepository<ProblemExample> problemExampleRepository,
        ICommandRepository<TestCase> tescaseRepository,
        IIdentityService identityService,
        IUnitOfWork unitOfWork,
        IJudge0ApiLogic judge0ApiLogic) 
    : IPracticeTestService
{
    public async Task<PracticeTestAdminLanguageInsertResponse> InsertPracticeLanguageAsync(PracticeTestAdminLanguageInsertRequest request, CancellationToken cancellationToken)
    {
        var response = new PracticeTestAdminLanguageInsertResponse { Success = false };
        
        var currentUser = identityService.GetCurrentUser()!;
     
        // Begin transaction
        await unitOfWork.BeginTransactionAsync(async () =>
        {
            // STEP 1: Get languages from Judge0 API
            var languageJudge0Selects = await judge0ApiLogic.GetLanguagesAsync();
            
            if (!languageJudge0Selects.Any())
            {
                response.SetMessage(MessageId.E00000, "Không thể lấy danh sách ngôn ngữ từ Judge0");
                return false;
            }
            
            // STEP 2: Get existing language IDs from database
            var existingLanguageIds = await codeLanguageRepository
                .Find(predicate: x => x.IsActive, isTracking: false)
                .Select(x => x.LanguageId)
                .ToListAsync(cancellationToken);
            
            // STEP 3: Filter out languages that already exist in database
            var newLanguages = languageJudge0Selects
                .Where(lang => !existingLanguageIds.Contains(lang.LanguageId))
                .ToList();
            
            if (!newLanguages.Any())
            {
                response.SetMessage(MessageId.I00001, "Tất cả ngôn ngữ lập trình đã tồn tại trong hệ thống");
                response.Success = true;
                response.Response = new PracticeTestAdminLanguageInsertResponseEntity
                {
                    TotalLanguagesFromJudge0 = languageJudge0Selects.Count,
                    ExistingLanguages = existingLanguageIds.Count,
                    NewLanguagesInserted = 0
                };
                return true;
            }
            
            // STEP 4: Insert new languages into database
            foreach (var language in newLanguages)
            {
                await codeLanguageRepository.AddAsync(language);
            }
            
            await unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken);
            
            // Success
            response.Success = true;
            response.Response = new PracticeTestAdminLanguageInsertResponseEntity
            {
                TotalLanguagesFromJudge0 = languageJudge0Selects.Count,
                ExistingLanguages = existingLanguageIds.Count,
                NewLanguagesInserted = newLanguages.Count
            };
            response.SetMessage(MessageId.I00001, $"Thêm thành công {newLanguages.Count} ngôn ngữ lập trình mới");
            return true;
        }, cancellationToken);
        return response;
    }

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

    /// <summary>
    /// Select User Stub Code
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<PracticeTestUserTemplateCodeSelectResponse> SelectUserStubCodeAsync(PracticeTestUserTemplateCodeSelectRequest request, CancellationToken cancellationToken)
    {
        var response = new PracticeTestUserTemplateCodeSelectResponse { Success = false };
        
        // Select problem stub code
        var problemTemplate = await problemTemplateRepository
            .Find(predicate: x => x.ProblemId == request.ProblemId && x.LanguageId == request.LanguageId && x.IsActive,
                isTracking: false,
                cancellationToken: cancellationToken)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
        if (problemTemplate == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy source code mẫu");
            return response;
        }
        
        // True
        response.Success = true;
        response.Response = new PracticeTestUserTemplateCodeSelectResponseEntity
        {
            UserTemplateCode = problemTemplate.UserStubCode
        };
        response.SetMessage(MessageId.I00001, "Lấy source code mẫu");
        return response;
    }
    
    /// <summary>
    /// Insert Practice Test for Admin
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<PracticeTestAdminInsertResponse> InsertPracticeTestAsync(PracticeTestAdminInsertRequest request, CancellationToken cancellationToken)
    {
         var response = new PracticeTestAdminInsertResponse { Success = false };
         
         var currentUser = identityService.GetCurrentUser()!;
         
         // Begin transaction
         await unitOfWork.BeginTransactionAsync(async () =>
         {
             // Create problem
             var problem = new Problem
             {
                 Title = request.Problem.Title,
                 Description = request.Problem.Description,
                 Difficulty = request.Problem.Difficulty
             };
             
             // Add test cases
             foreach (var publicTestcase in request.Testcases.FirstOrDefault()?.PublicTestcases ?? new List<PracticeTestAdminProblemTestcasePublicInsertRequest>())
             {
                 problem.TestCases.Add(new TestCase
                 {
                     ProblemId = problem.ProblemId,
                     InputData = publicTestcase.InputData,
                     ExpectedOutput = publicTestcase.ExpectedOutput,
                     IsPublic = true
                 });
             }
             
             foreach (var privateTestcase in request.Testcases.FirstOrDefault()?.PrivateTestcases ?? new List<PracticeTestAdminProblemTestcasePrivateInsertRequest>())
             {
                 problem.TestCases.Add(new TestCase
                 {
                     ProblemId = problem.ProblemId,
                     InputData = privateTestcase.InputData,
                     ExpectedOutput = privateTestcase.ExpectedOutput,
                     IsPublic = false
                 });
             }
             
             // Add templates
             foreach (var template in request.Templates)
             {
                 problem.ProblemTemplates.Add(new ProblemTemplate
                 {
                     ProblemId = problem.ProblemId,
                     LanguageId = template.LanguageId,
                     TemplatePrefix = template.UserTemplatePrefix,
                     TemplateSuffix = template.UserTemplateSuffix,
                     UserStubCode = template.UserStubCode
                 });
             }
             
             // Add examples
             foreach (var example in request.Examples)
             {
                 problem.ProblemExamples.Add(new ProblemExample
                 {
                     ProblemId = problem.ProblemId,
                     ExampleOrder = example.ExampleOrder,
                     InputData = example.InputData,
                     OutputData = example.OutputData,
                     Explanation = example.Explanation
                 });
             }
             
             await problemCommandRepository.AddAsync(problem);
             await unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken);
             
             // True
             response.Success = true;
             response.SetMessage(MessageId.I00001, "Tạo bài kiểm tra thực hành mới");
             return true;
         }, cancellationToken);
         
        return response;
    }

    /// <summary>
    /// Update Practice Test for Admin
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<PracticeTestAdminUpdateResponse> UpdatePracticeTestAsync(PracticeTestAdminUpdateRequest request, CancellationToken cancellationToken)
    {
        var response = new PracticeTestAdminUpdateResponse { Success = false };
        
        var currentUser = identityService.GetCurrentUser()!;
        
        // Check problem exists
        var problem = await problemCommandRepository
            .Find(predicate: x => x.ProblemId == request.ProblemId && x.IsActive,
                isTracking: true,
                cancellationToken: cancellationToken,
                x => x.TestCases,
                x => x.ProblemTemplates,
                x => x.ProblemExamples)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (problem == null)
        {
            response.SetMessage(MessageId.E00000, CommonMessages.NotFoundPracticeTestMessage);
            return response;
        }
        
        // Begin transaction
        await unitOfWork.BeginTransactionAsync(async () =>
        {
            // STEP 1: Update problem basic info
            if (!string.IsNullOrWhiteSpace(request.Problem.Title) && request.Problem.Title != null)
                problem.Title = request.Problem.Title;
            
            if (!string.IsNullOrWhiteSpace(request.Problem.Description) && request.Problem.Description != null)
                problem.Description = request.Problem.Description;
            
            if (!string.IsNullOrWhiteSpace(request.Problem.Difficulty) && request.Problem.Difficulty != null)
                problem.Difficulty = request.Problem.Difficulty;
            
            // STEP 2: Update test cases (only update existing items)
            if (request.Testcases != null && request.Testcases.Any())
            {
                foreach (var testcaseRequest in request.Testcases)
                {
                    var existingTestcase = problem.TestCases.FirstOrDefault(t => t.TestcaseId == testcaseRequest.TestcaseId);
                    if (existingTestcase != null)
                    {
                        existingTestcase.InputData = testcaseRequest.InputData;
                        existingTestcase.ExpectedOutput = testcaseRequest.ExpectedOutput;
                        existingTestcase.IsPublic = testcaseRequest.IsPublic;
                    }
                }
            }
            
            // STEP 3: Update templates (only update existing items)
            if (request.Templates != null && request.Templates.Any())
            {
                foreach (var templateRequest in request.Templates)
                {
                    var existingTemplate = problem.ProblemTemplates.FirstOrDefault(t => t.TemplateId == templateRequest.TemplateId);
                    if (existingTemplate != null)
                    {
                        existingTemplate.LanguageId = templateRequest.LanguageId;
                        existingTemplate.TemplatePrefix = templateRequest.UserTemplatePrefix;
                        existingTemplate.TemplateSuffix = templateRequest.UserTemplateSuffix;
                        existingTemplate.UserStubCode = templateRequest.UserStubCode;
                    }
                }
            }
            
            // STEP 4: Update examples (only update existing items)
            if (request.Examples != null && request.Examples.Any())
            {
                foreach (var exampleRequest in request.Examples)
                {
                    var existingExample = problem.ProblemExamples.FirstOrDefault(e => e.ExampleId == exampleRequest.ExampleId);
                    if (existingExample != null)
                    {
                        existingExample.ExampleOrder = exampleRequest.ExampleOrder;
                        existingExample.InputData = exampleRequest.InputData;
                        existingExample.OutputData = exampleRequest.OutputData;
                        existingExample.Explanation = exampleRequest.Explanation;
                    }
                }
            }
            
            //  Save changes
            problemCommandRepository.Update(problem);
            await unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken);
            
            // Mark test cases not in request as inactive
            if (request.Testcases != null)
            {
                if (request.Testcases.Any())
                {
                    var requestTestcaseIds = request.Testcases.Select(t => t.TestcaseId).ToList();
                    var testcasesToRemove = problem.TestCases
                        .Where(t => t.IsActive && !requestTestcaseIds.Contains(t.TestcaseId))
                        .ToList();
                    
                    foreach (var testcase in testcasesToRemove)
                    {
                        tescaseRepository.Update(testcase);
                    }
                }
                else
                {
                    // If empty list is sent, delete all testcases but keep at least 1
                    var activeTestcases = problem.TestCases.Where(t => t.IsActive).ToList();
                    if (activeTestcases.Count > 1)
                    {
                        foreach (var testcase in activeTestcases.Skip(1))
                        {
                            tescaseRepository.Update(testcase);
                        }
                    }
                    else
                    {
                        response.SetMessage(MessageId.E00000, "Phải có ít nhất 1 test case còn tồn tại trong hệ thống");
                        return false;
                    }
                }
                await unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken, true);
            }
            
            // Mark templates not in request as inactive
            if (request.Templates != null && request.Templates.Any())
            {
                var requestTemplateIds = request.Templates.Select(t => t.TemplateId).ToList();
                var templatesToRemove = problem.ProblemTemplates
                    .Where(t => t.IsActive && !requestTemplateIds.Contains(t.TemplateId))
                    .ToList();
                
                foreach (var template in templatesToRemove)
                {
                    problemTemplateRepository.Update(template);
                }
                await unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken, true);
            }
            
            // Mark examples not in request as inactive
            if (request.Examples != null)
            {
                if (request.Examples.Any())
                {
                    var requestExampleIds = request.Examples.Select(e => e.ExampleId).ToList();
                    var examplesToRemove = problem.ProblemExamples
                        .Where(e => e.IsActive && !requestExampleIds.Contains(e.ExampleId))
                        .ToList();
                    
                    foreach (var example in examplesToRemove)
                    {
                        problemExampleRepository.Update(example);
                    }
                }
                else
                {
                    // If empty list is sent, delete all examples but keep at least 1
                    var activeExamples = problem.ProblemExamples.Where(e => e.IsActive).ToList();
                    if (activeExamples.Count > 1)
                    {
                        foreach (var example in activeExamples.Skip(1))
                        {
                            problemExampleRepository.Update(example);
                        }
                    }
                    else
                    {
                        response.SetMessage(MessageId.E00000, "Phải có ít nhất 1 ví dụ mẫu còn tồn tại trong hệ thống");
                        return false;
                    }
                }
                await unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken, true);
            }
            
            // STEP 6: Clear cache
            var cacheKey = CacheKey.PracticeTestSelect(problem.ProblemId);
            await unitOfWork.CacheRemoveAsync(cacheKey);
            
            // Success
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Cập nhật bài kiểm tra thực hành");
            return true;
        }, cancellationToken);
        
        return response;
    }

    /// <summary>
    /// Delete Practice Test for Admin (Soft Delete)
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<PracticeTestAdminDeleteResponse> DeletePracticeTestAsync(PracticeTestAdminDeleteRequest request, CancellationToken cancellationToken)
    {
        var response = new PracticeTestAdminDeleteResponse { Success = false };
        
        var currentUser = identityService.GetCurrentUser()!;
        
        // Check problem exists and load all related entities
        var problem = await problemCommandRepository
            .Find(predicate: x => x.ProblemId == request.ProblemId && x.IsActive,
                isTracking: true,
                cancellationToken: cancellationToken,
                x => x.TestCases,
                x => x.ProblemTemplates,
                x => x.ProblemExamples)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (problem == null)
        {
            response.SetMessage(MessageId.E00000, CommonMessages.NotFoundPracticeTestMessage);
            return response;
        }
        
        // Begin transaction
        await unitOfWork.BeginTransactionAsync(async () =>
        {
            // Mark all test cases as inactive
            foreach (var testCase in problem.TestCases.Where(t => t.IsActive))
            {
                tescaseRepository.Update(testCase);
            }
            
            // Mark all templates as inactive
            foreach (var template in problem.ProblemTemplates.Where(t => t.IsActive))
            {
                problemTemplateRepository.Update(template);
            }
            
            // Mark all examples as inactive
            foreach (var example in problem.ProblemExamples.Where(e => e.IsActive))
            {
                problemExampleRepository.Update(example);
            }
            
            // Soft delete the problem itself
            problemCommandRepository.Update(problem);
            await unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken, needLogicalDelete: true);
            
            // Clear cache
            var cacheKey = CacheKey.PracticeTestSelect(problem.ProblemId);
            await unitOfWork.CacheRemoveAsync(cacheKey);
            
            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Xóa bài kiểm tra thực hành và tất cả dữ liệu liên quan");
            return true;
        }, cancellationToken);
        
        return response;
    }

    /// <summary>
    /// Add Testcases to Practice Test for Admin
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<PracticeTestAdminTestcasesInsertResponse> InsertPracticeTestTestcasesAsync(PracticeTestAdminTestcasesInsertRequest request, CancellationToken cancellationToken)
    {
        var response = new PracticeTestAdminTestcasesInsertResponse { Success = false };
        
        var currentUser = identityService.GetCurrentUser()!;
        
        // Check problem exists
        var problem = await problemCommandRepository
            .Find(predicate: x => x.ProblemId == request.ProblemId && x.IsActive,
                isTracking: true,
                cancellationToken: cancellationToken)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (problem == null)
        {
            response.SetMessage(MessageId.E00000, CommonMessages.NotFoundPracticeTestMessage);
            return response;
        }
        
        // Begin transaction
        await unitOfWork.BeginTransactionAsync(async () =>
        {
            // Add public test cases
            if (request.PublicTestcases != null && request.PublicTestcases.Any())
            {
                foreach (var testcase in request.PublicTestcases)
                {
                    var newTestcase = new TestCase
                    {
                        ProblemId = problem.ProblemId,
                        InputData = testcase.InputData,
                        ExpectedOutput = testcase.ExpectedOutput,
                        IsPublic = true
                    };
                    await tescaseRepository.AddAsync(newTestcase);
                }
            }
            
            // Add private test cases
            if (request.PrivateTestcases != null && request.PrivateTestcases.Any())
            {
                foreach (var testcase in request.PrivateTestcases)
                {
                    var newTestcase = new TestCase
                    {
                        ProblemId = problem.ProblemId,
                        InputData = testcase.InputData,
                        ExpectedOutput = testcase.ExpectedOutput,
                        IsPublic = false
                    };
                    await tescaseRepository.AddAsync(newTestcase);
                }
            }
            await unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken);
            
            // Clear cache
            var cacheKey = CacheKey.PracticeTestSelect(problem.ProblemId);
            await unitOfWork.CacheRemoveAsync(cacheKey);
            
            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Thêm test cases");
            return true;
        }, cancellationToken);
        
        return response;
    }

    /// <summary>
    /// Add Templates to Practice Test for Admin
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<PracticeTestAdminTemplatesInsertResponse> InsertPracticeTestTemplatesAsync(PracticeTestAdminTemplatesInsertRequest request, CancellationToken cancellationToken)
    {
        var response = new PracticeTestAdminTemplatesInsertResponse { Success = false };
        
        var currentUser = identityService.GetCurrentUser()!;
        
        // Check problem exists
        var problem = await problemCommandRepository
            .Find(predicate: x => x.ProblemId == request.ProblemId && x.IsActive,
                isTracking: true,
                cancellationToken: cancellationToken)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (problem == null)
        {
            response.SetMessage(MessageId.E00000, CommonMessages.NotFoundPracticeTestMessage);
            return response;
        }
        
        // Begin transaction
        await unitOfWork.BeginTransactionAsync(async () =>
        {
            // Add templates
            if (request.Templates.Any())
            {
                foreach (var template in request.Templates)
                {
                    // Check if template for this language already exists
                    var existingTemplate = problem.ProblemTemplates
                        .FirstOrDefault(t => t.LanguageId == template.LanguageId && t.IsActive);
                    
                    if (existingTemplate != null)
                    {
                        response.SetMessage(MessageId.E00000, $"Template cho ngôn ngữ ID {template.LanguageId} đã tồn tại");
                        return false;
                    }
                    
                    var newProblemTemplate = new ProblemTemplate
                    {
                        ProblemId = problem.ProblemId,
                        LanguageId = template.LanguageId,
                        TemplatePrefix = template.UserTemplatePrefix,
                        TemplateSuffix = template.UserTemplateSuffix,
                        UserStubCode = template.UserStubCode
                    };
                    await problemTemplateRepository.AddAsync(newProblemTemplate);
                }
            }
            await unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken);
            
            // Clear cache
            var cacheKey = CacheKey.PracticeTestSelect(problem.ProblemId);
            await unitOfWork.CacheRemoveAsync(cacheKey);
            
            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Thêm code mẫu");
            return true;
        }, cancellationToken);
        
        return response;
    }

    /// <summary>
    /// Add Examples to Practice Test for Admin
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<PracticeTestAdminExamplesInsertResponse> InsertPracticeTestExamplesAsync(PracticeTestAdminExamplesInsertRequest request, CancellationToken cancellationToken)
    {
        var response = new PracticeTestAdminExamplesInsertResponse { Success = false };
        
        var currentUser = identityService.GetCurrentUser()!;
        
        // Check problem exists
        var problem = await problemCommandRepository
            .Find(predicate: x => x.ProblemId == request.ProblemId && x.IsActive,
                isTracking: true,
                cancellationToken: cancellationToken)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (problem == null)
        {
            response.SetMessage(MessageId.E00000, CommonMessages.NotFoundPracticeTestMessage);
            return response;
        }
        
        // Begin transaction
        await unitOfWork.BeginTransactionAsync(async () =>
        {
            // Add examples
            if (request.Examples.Any())
            {
                foreach (var example in request.Examples)
                {
                    var newProblemExample = new ProblemExample
                    {
                        ProblemId = problem.ProblemId,
                        ExampleOrder = example.ExampleOrder,
                        InputData = example.InputData,
                        OutputData = example.OutputData,
                        Explanation = example.Explanation
                    };
                    await problemExampleRepository.AddAsync(newProblemExample);
                }
            }
            
            await unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken);
            
            // Clear cache
            var cacheKey = CacheKey.PracticeTestSelect(problem.ProblemId);
            await unitOfWork.CacheRemoveAsync(cacheKey);
            
            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Thêm ví dụ mẫu");
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