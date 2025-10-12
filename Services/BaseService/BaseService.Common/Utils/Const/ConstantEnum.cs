namespace BaseService.Common.Utils.Const;

public static class ConstantEnum
{
    public enum UserRole
    {
        Student = 1,
        Lecturer = 2,
        Admin = 3,
    }

    public enum TestType
    {
        Survey = 1,
        Quiz = 2,
        Exam = 3,
    }

    public enum PaymentStatus
    {
        Pending = 1,
        Paid = 2,
        Failed = 3,
    }

    public enum PaymentMethod
    {
        Cash = 1,
        Momo = 2,
        PayOs = 3,
    }

    public enum QuestionType
    {
        MultipleChoice = 1,
        TrueFalse = 2,
        SingleChoice = 3,
    }

    public enum TechnologyType
    {
        ProgrammingLanguage = 1,
        Framework = 2,
        Database = 3,
    }

    public enum LearningGoalType
    {
        None = 0,
        Frontend = 2,
        Backend = 3,
        Fullstack = 4
    }

    public enum SurveyCode
    {
        INTEREST,
        HABIT
    }

    public enum LearningPathMajor
    {
        Basic = 1,
        Internal = 2,
        External = 3,
    }

    public enum AnswerRuleUnit
    {
        HourPerDay = 1,
        HourPerWeek = 2,
        Days = 3,
        Months = 4,
    }

    public enum LessonStatus
    {
        NotStarted = 0,
        InProgress = 1,
        Completed = 2
    }

	public enum CourseStatus
	{
		NotStarted = 0, // Nguoi hoc chua mo lesson nao trong course
		InProgress = 1, // Nguoi hoc da bat dau it nhat mot lesson
		Completed = 2,
		Archived = 3 // khong con active de hoc moi, nhung nguoi hoc cu van thay trong profile
	}

	public enum LearningPathStatus
    {
        Choosing = 0, // In Choosing status for choosing external and internal
        InProgress = 1, // Studying course status
        Completed = 2, // Complete Learning Path
        Closed = 3, // Close Learning Path to change other learning Path
    }

    public enum UserBehaviourActionType
    {
        // Course actions
        ViewCourse = 1,
        EnrollCourse = 2,
        CompleteCourse = 3,
        PauseCourse = 4,
        
        // Lesson actions
        ViewLesson = 10,
        StartLesson = 11,
        CompleteLesson = 12,
        PauseLesson = 13,
        ResumeLesson = 14,
        SkipLesson = 15,
        
        // Video actions
        PlayVideo = 20,
        PauseVideo = 21,
        SeekVideo = 22,
        CompleteVideo = 23,
        
        // Quiz/Test actions
        StartQuiz = 30,
        SubmitQuiz = 31,
        ViewQuizResult = 32,
        RetakeQuiz = 33,
        
        // Learning path actions
        ViewLearningPath = 40,
        StartLearningPath = 41,
        CompleteLearningPath = 42,
        
        // Search & Navigation
        Search = 50,
        ClickSearchResult = 51,
        Navigate = 52,
        
        // Content interaction
        Like = 60,
        Unlike = 61,
        Bookmark = 62,
        RemoveBookmark = 63,
        Share = 64,
        Comment = 65,
        
        // System actions
        Login = 70,
        Logout = 71,
        UpdateProfile = 72,
    }

    public enum UserBehaviourTargetType
    {
        Course = 1,
        Lesson = 2,
        Quiz = 4,
        Test = 5,
        LearningPath = 6,
        SearchQuery = 7,
        Profile = 8,
        Other = 99,
    }

	public enum QuizScope { Lesson = 1, Module = 2 }
}