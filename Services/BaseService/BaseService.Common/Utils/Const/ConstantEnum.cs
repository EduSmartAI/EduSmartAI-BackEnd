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
		InProgress = 1,
		Completed = 2
	}
}