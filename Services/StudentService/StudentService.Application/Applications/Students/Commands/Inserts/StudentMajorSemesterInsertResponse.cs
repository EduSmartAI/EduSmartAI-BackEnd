using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;
using MediatR;

namespace StudentService.Application.Applications.Students.Commands.Inserts;

public record StudentMajorSemesterInsertResponse : AbstractApiResponse<string>, ICommand<Unit>
{
    public override string Response { get; set; }
}