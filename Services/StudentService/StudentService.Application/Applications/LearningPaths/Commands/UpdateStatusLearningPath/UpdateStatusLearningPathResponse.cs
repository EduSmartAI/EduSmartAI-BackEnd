using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.LearningPaths.Commands.UpdateStatusLearningPath
{
    public record UpdateStatusLearningPathResponse : AbstractApiResponse<string>
    {
        public override string Response { get; set; }
    }
}
