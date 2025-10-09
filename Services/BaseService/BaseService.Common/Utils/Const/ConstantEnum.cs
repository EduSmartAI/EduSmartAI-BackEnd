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
        NotStarted = 0, // Ng??i h?c ch?a m? lesson nào trong course
        InProgress = 1, // Ng??i h?c ?ã b?t ??u ít nh?t m?t lesson
        Completed = 2,
        Archived = 3 // không còn active ?? h?c m?i, nh?ng ng??i h?c c? v?n th?y trong profile
    }

    public enum LearningPathStatus
    {
        Choosing = 0, // In Choosing status for choosing external and internal
        InProgress = 1, // Studying course status
        Completed = 2, // Complete Learning Path
        Closed = 3, // Close Learning Path to change other learning Path
    }
}