using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.LearningPaths.Commands
{
    public record LearningPathInsertResponse : AbstractApiResponse<string>
    {
        public override string Response { get; set; }
    }
}
