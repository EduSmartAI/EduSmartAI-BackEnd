using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.LearningPathsMajor.Commands.InsertLearningPathsMajor
{
    public record InsertLearningPathsMajorResponse : AbstractApiResponse<string>
    {
        public override string Response { get; set; } = string.Empty;
    }
}
