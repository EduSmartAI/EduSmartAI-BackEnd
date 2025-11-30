namespace BaseService.Common.Utils.Const;

public static class CacheKey
{
    public static string StudentTest(Guid studentTestId)
        => $"studentTest:{studentTestId}";
    
    public static string AdminStudentTest(Guid studentTestId)
        => $"admin_studentTest:{studentTestId}";

    public static string StudentMajorSemesterInformation(Guid studentMajorSemesterId)
        => $"studentMajorSemesterInformation:{studentMajorSemesterId}";
    
    public static string StudentSurvey(Guid studentSurveyId)
        => $"studentSurvey:{studentSurveyId}";
    
    public static string SurveyDetail(Guid surveyId)
        => $"survey:{surveyId}";

    public static string StudentQuizCourse(Guid studentId, Guid quizId)
        => $"studentQuizCourse:{studentId}-{quizId}";

    public static string QuizCourses(Guid existingQuizQuizId)
        => $"QuizCourse_{existingQuizQuizId}";

    public static string UserBehaviours(Guid userId)
        => $"user_behaviour:all:{userId}";

    public static string LearningGoalSelects()
        => "learning_goals:all";

    public static string QuizList()
        => "quiz:list";

    public static string StudentProfile(Guid studentId)
        => $"student_profile:{studentId}";

    public static string StudentTranscript(Guid userId)
        => $"student_transcript:{userId}";

    public static string PracticeTestSelects()
        => "practice_test:selects";
    
    public static string PracticeTestSelect(Guid problemId)
        => $"practice_test:{problemId}";
    
    public static string LearningPath(Guid pathId)
        => $"learning_path:{pathId}";
    
    public static string LearningPathSelect(Guid studentId, Guid pathId)
        => $"learning_path:select:{studentId}:{pathId}";
    
    public static string LearningPathMajorList(Guid pathId)
        => $"learning_path_major:list:{pathId}";
    
    public static string LearningPathPaged(Guid studentId, int pageNumber, int pageSize)
        => $"learning_path:paged:{studentId}:p{pageNumber}:s{pageSize}";
}