namespace BaseService.Common.Utils.Const;

public static class CacheKey
{
    public static string StudentTest(Guid studentTestId)
        => $"studentTest:{studentTestId}";

    public static string StudentMajorSemesterInformation(Guid studentMajorSemesterId)
        => $"studentMajorSemesterInformation:{studentMajorSemesterId}";
    
    public static string StudentSurvey(Guid studentSurveyId)
        => $"studentSurvey:{studentSurveyId}";

    public static string StudentQuizCourse(Guid studentId, Guid quizId)
        => $"studentQuizCourse:{studentId}-{quizId}";
}