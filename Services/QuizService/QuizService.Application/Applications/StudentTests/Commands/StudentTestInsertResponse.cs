using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.StudentTests.Commands;

public record StudentTestInsertResponse : AbstractApiResponse<Guid?>
{
    public override Guid? Response { get; set; }
}