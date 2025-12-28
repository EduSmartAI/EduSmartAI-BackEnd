using System.ComponentModel;

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
        SystemError = 4,
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
    }

    public enum LearningGoalType
    {
        None = 0,
        Frontend = 2,
        Backend = 3,
        Fullstack = 4,
        Mobile = 5,
        Devops = 6,
        DataScience = 7,
        AI = 8,
        CloudComputing = 9,
        CyberSecurity = 10
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
        Generating = 0,
        Choosing = 1,
        InProgress = 2,
        Completed = 3,
        Closed = 4,
        Paused = 5
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
        RetryLesson = 16,

        // Video actions
        PlayVideo = 20,
        PauseVideo = 21,
        SeekVideo = 22,
        CompleteVideo = 23,
        ScrollVideo = 24,

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

    public enum QuizScope
    {
        Lesson = 1,
        Module = 2,
        Overview = 3
    }

    public enum StudentLearningPathCourseStatus
    {
        NotStarted = 0,
        InProgress = 1,
        Completed = 2,
        // Skipped = 3,
        NoCourse = 4,
    }

    public enum SuggestionType
    {
        LowerLevel = 1,
        SameLevel = 2,
        HigherLevel = 3
    }

    // Course Suggestion Constants
    public const decimal SUGGESTION_THRESHOLD_PERCENTAGE = 0.4m;
    public const decimal PASSING_SCORE = 4.0m;

    public enum ModuleProgressStatus : short
    {
        NotStarted = 0,
        InProgress = 1,
        Completed = 2
    }

    public enum Gender
    {
        Male = 1,
        Female = 2,
        Other = 3,
        None = 4
    }

    public enum TranscriptStatus : short
    {
        Succeeded = 1,
        Failed = 2,
    }

    public enum PracticeTestSubmissionStatus
    {
        Pass = 1,
        Failed = 2,
        RuntimeError = 3,
        TimeOut = 4,
    }

    public enum Judge0Status
    {
        InQueue = 1,
        Processing = 2,
        Accepted = 3,
        WrongAnswer = 4,
        TimeLimitExceeded = 5,
        CompilationError = 6,
        RuntimeErrorSIGSEGV = 7,
        RuntimeErrorSIGXFSZ = 8,
        RuntimeErrorSIGFPE = 9,
        RuntimeErrorSIGABRT = 10,
        RuntimeErrorNZEC = 11,
        RuntimeErrorOther = 12,
        InternalError = 13,
        ExecFormatError = 14
    }

    public enum CourseLevel
    {
        Beginner = 1,
        Intermidiate = 2,
        Advanced = 3
    }
    public enum OverviewTypeRequest
    {
        StudentOverview = 1,
        Stats = 2,
    }
    public enum OutboxEnvType
    {
        Production = 1,
        Development = 2,
    }
    public enum LearningTimeSlot : short
    {
        None = 0,
        Morning = 1,
        Afternoon = 2,
        Evening = 3,
        LateNight = 4
    }

    public enum ProblemDifficultyLevel
    {
        Easy = 1,
        Medium = 2,
        Hard = 3
    }

    public enum CourseProgressStatus : short
    {
        NotStarted = 0,
        InProgress = 1,
        Completed = 2
    }

    public enum CartStatus : short
    {
        /// <summary>
        /// Cart dang hoat dong, user co the them/xoa item.
        /// </summary>
        Active = 0,

        /// <summary>
        /// Mot phan hoac toan bo cart da duoc chuyen thanh order.
        /// </summary>
        ConvertedToOrder = 1,

        /// <summary>
        /// User khong su dung cart nua hoac qua lau khong tuong tac.
        /// </summary>
        Abandoned = 2
    }

    public enum CartItemStatus : short
    {
        /// <summary>
        /// Item dang nam trong cart va co the checkout.
        /// </summary>
        Active = 0,

        /// <summary>
        /// User xoa item khoi cart (soft delete).
        /// </summary>
        Removed = 1,

        /// <summary>
        /// Item da duoc thanh toan va chuyen thanh order item.
        /// </summary>
        Purchased = 2
    }

    public enum OrderStatus : short
    {
        /// <summary>
        /// Order moi duoc tao, chua gui sang cong thanh toan.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Da tao payment transaction va redirect sang gateway.
        /// </summary>
        WaitingForPayment = 1,

        /// <summary>
        /// Thanh toan thanh cong, da nhan callback xac thuc.
        /// </summary>
        Paid = 2,

        /// <summary>
        /// Order bi user huy hoac timeout truoc khi thanh toan.
        /// </summary>
        Cancelled = 3,

        /// <summary>
        /// Thanh toan that bai (gateway tra ve error hoac verify signature fail).
        /// </summary>
        Failed = 4,

        /// <summary>
        /// Order da duoc hoan tien (full hoac partial).
        /// </summary>
        Refunded = 5
    }

    public enum PaymentGateway
    {
        Momo = 1,
        PayOs = 2
    }

    /// <summary>
    /// Status lấy từ PayOs nên chỗ này không đổi tên của các giá trị enum
    /// </summary>
    public enum PaymentReturnCode
    {
        PENDING = 1,
        CANCELLED = 2,
        UNDERPAID = 3,
        PAID = 4,
        EXPIRED = 5,
        PROCESSING = 6,
        FAILED = 7
    }

    public enum StudentTranscriptStatus
    {
        [Description("Not Started")]
        NotStarted = 0,

        [Description("Studying")]
        Studying = 1,

        [Description("Passed")]
        Passed = 2,

        [Description("Not Passed")]
        NotPassed = 3
    }

    public enum UserActionPayment
    {
        [Description("User cancelled the payment")]
        Cancelled = 1,

        [Description("Payment completed successfully")]
        Success = 2,

        [Description("Payment process failed")]
        Failed = 3
    }

    public enum OtherQuestionCode
    {
        [Description("Điểm của bạn có môn từ 5 đến dưới 7, bạn có muốn học thêm khóa học để cải thiện không?")]
        GRADE_5_TO_7_COURSE = 1,

        [Description("Điểm của bạn có môn từ 7 đến dưới 8, bạn có muốn học thêm khóa học để cải thiện không?")]
        GRADE_7_TO_8_COURSE = 2,

        [Description("Điểm của bạn có môn từ 8 đến 9, bạn có muốn học thêm khóa học để cải thiện thêm kiến thức không?")]
        GRADE_8_TO_9_COURSE = 3,

        [Description("Điểm của bạn có môn từ 5 đến dưới 7, bạn có muốn chúng tôi đánh giá để cải thiện về các môn đó không?")]
        GRADE_5_TO_7_EVALUATION = 4,

        [Description("Điểm của bạn có môn từ 7 đến dưới 8, bạn có muốn chúng tôi đánh giá để cải thiện về các môn đó không?")]
        GRADE_7_TO_8_EVALUATION = 5
    }

    public enum SuggestedCourseType : short
    {
        Easier = 1,
        Harder = 2
    }
    public enum ChatBotRawReason
    {
        GetAllLearningPath = 1,
        GetDetailTrainingPath = 2,
        SkipSubjectLearningPath = 3,
    }

    public enum SubjectImprovementStatus
    {
        [Description("Đã đạt và đang cải thiện")]
        PassedAndImproving = 1,
        
        [Description("Chưa đạt và đang cải thiện")]
        NotPassedAndImproving = 2,
        
        [Description("Chưa học trên FAP và đang học khoá học")]
        NotStartedAndImproving = 4,
        
        [Description("Đã đạt và chỉ đánh giá")]
        PassedAndEvaluation = 5,
        
        [Description("Đã đạt điểm tốt")]
        PassedWithGoodGrade = 6,
         
        [Description("Đã hoàn thành")]
        Completed = 7,
        
        [Description("Bỏ qua")]
        Skipped = 8,
        [Description("Đang học")]
        Studying = 9
    }
}