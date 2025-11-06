namespace QuizService.Application.Judge0Logics.Models;

public class BatchSubmissionRequest
{
    public List<SubmissionRequest> Submissions { get; set; }
}

public class BatchSubmissionResponse
{
    public List<SubmissionResponse> Submissions { get; set; }
}

public class BatchSubmissionResult
{
    public List<SubmissionResult> Submissions { get; set; }
}