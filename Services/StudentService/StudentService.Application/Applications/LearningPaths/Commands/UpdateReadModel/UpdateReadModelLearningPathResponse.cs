using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.LearningPaths.Commands.UpdateReadModel
{
    public record UpdateReadModelLearningPathResponse : AbstractApiResponse<string>
    {
        public override string Response { get; set; }
    }
}
