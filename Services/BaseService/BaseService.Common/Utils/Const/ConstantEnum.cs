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
        ShortAnswer = 3,
        SingleChoice = 4,
    }
    
    public enum TechnologyType
    {
        ProgrammingLanguage = 1,
        Framework = 2,
        Tool = 3,
        Platform = 4,
        Database = 5,
        Other = 6,
    }
    
    public enum LearningGoalType
    {
        None = 1,
        Frontend = 2,
        Backend = 3,
        Fullstack = 4
    }
}