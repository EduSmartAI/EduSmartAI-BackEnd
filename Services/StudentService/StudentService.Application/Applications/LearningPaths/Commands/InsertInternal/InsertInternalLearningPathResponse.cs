using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.LearningPaths.Commands.InsertInternal
{
    public record InsertInternalLearningPathResponse : AbstractApiResponse<string>
    {
        public override string Response { get; set; }
    }
}
