using System;
using System.Collections.Generic;

namespace QuizService.Domain.WriteModels;

public partial class SurveyType
{
    public short SurveyTypeId { get; set; }

    public string SurveyTypeName { get; set; } = null!;

    public string? Description { get; set; }

    public string SurveyCode { get; set; } = null!;

    public virtual ICollection<SurveyQuizSetting> SurveyQuizSettings { get; set; } = new List<SurveyQuizSetting>();
}
