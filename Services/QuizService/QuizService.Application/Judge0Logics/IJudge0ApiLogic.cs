using QuizService.Application.Judge0Logics.Models;
using QuizService.Domain.WriteModels;

namespace QuizService.Application.Judge0Logics;

public interface IJudge0ApiLogic
{
    Task<SubmissionResponse> SubmitCodeAsync(SubmissionRequest request);

    Task<SubmissionResult> SubmitAndWaitAsync(SubmissionRequest request);

    Task<SubmissionResult> GetSubmissionAsync(string token);

    Task<BatchSubmissionResponse> SubmitBatchAsync(BatchSubmissionRequest request);

    Task<List<SubmissionResult>> GetBatchSubmissionAsync(string tokens);

    Task<List<CodeLanguage>> GetLanguagesAsync();
}