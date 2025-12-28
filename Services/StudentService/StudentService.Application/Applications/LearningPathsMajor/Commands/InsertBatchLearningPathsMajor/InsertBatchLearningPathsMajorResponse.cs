using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.LearningPathsMajor.Commands.InsertBatchLearningPathsMajor;

public record InsertBatchLearningPathsMajorResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; } = string.Empty;
}

