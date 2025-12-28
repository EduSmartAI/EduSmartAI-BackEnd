using QuizService.Domain.WriteModels;

namespace QuizService.Domain.ReadModels;

public sealed class QuizCollection
{
    public Guid QuizId { get; set; }
    
    public short QuizType { get; set; }
    
    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }
    
    public PlacementTestQuizSettingCollection? PlacementTestQuizSetting { get; set; }
    
    public CourseQuizSettingCollection? CourseQuizSetting { get; set; }
    
    public SurveyQuizSettingCollection? SurveyQuizSetting { get; set; }

    public List<QuestionCollection> Questions { get; set; }
    public static QuizCollection FromWriteModel(Quiz model, SurveyType? surveyType = null)
    {
        var quiz = new QuizCollection
        {
            QuizId = model.QuizId,
            QuizType = model.QuizType,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive
        };

        if (model.Questions.Any())
        {
            quiz.Questions = model.Questions.Select(QuestionCollection.FromWriteModel).ToList();
        }
        
        // Survey = 1, PlacementTest = 2, Course Quiz = 3
        // If QuizType is Survey (1), map SurveyQuizSetting
        if (model is { QuizType: 1, SurveyQuizSetting: not null } && surveyType != null)
        {
            quiz.SurveyQuizSetting = new SurveyQuizSettingCollection
            {
                SurveyTypeId = model.SurveyQuizSetting.SurveyTypeId,
                SurveyCode = surveyType.SurveyCode,
                SurveyTypeName = surveyType.SurveyTypeName,
                Title = model.SurveyQuizSetting.Title,
                Description = model.SurveyQuizSetting.Description,
            };
        }
       
        // If QuizType is Course Quiz (3), map CourseQuizSetting
        else if (model is { QuizType: 3, CourseQuizSetting: not null })
        {
            quiz.CourseQuizSetting = new CourseQuizSettingCollection
            {
                QuizId = model.CourseQuizSetting.QuizId,
                DurationMinutes = model.CourseQuizSetting.DurationMinutes,
                PassingScorePercentage = model.CourseQuizSetting.PassingScorePercentage,
                ShuffleQuestions = model.CourseQuizSetting.ShuffleQuestions,
                ShowResultsImmediately = model.CourseQuizSetting.ShowResultsImmediately,
                AllowRetake = model.CourseQuizSetting.AllowRetake,
            };
        }

        return quiz;
    }
    
    public static QuizCollection FromWriteModel(Quiz model, string subjectCodeName)
    {
        var quiz = new QuizCollection
        {
            QuizId = model.QuizId,
            QuizType = model.QuizType,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive
        };

        if (model.Questions.Any())
        {
            quiz.Questions = model.Questions.Select(QuestionCollection.FromWriteModel).ToList();
        }
        
        // If QuizType is PlacementTest (2), map PlacementTestQuizSetting
        if (model is { QuizType: 2, PlacementTestQuizSetting: not null })
        {
            quiz.PlacementTestQuizSetting = new PlacementTestQuizSettingCollection
            {
                SubjectCode = model.PlacementTestQuizSetting.SubjectCode,
                SubjectCodeName = subjectCodeName,
                Title = model.PlacementTestQuizSetting.Title,
                Description = model.PlacementTestQuizSetting.Description,
            };
        }
        
        return quiz;
    }
}