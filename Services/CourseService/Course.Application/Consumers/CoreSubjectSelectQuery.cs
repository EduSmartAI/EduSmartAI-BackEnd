using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.CourseService;
using Course.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Course.Application.Consumers;

public class CoreSubjectSelectQuery : IRequest<CoreSubjectSelectEventResponse>
{
    public List<string> SubjectCodes { get; set; } = null!;
}

public class CoreSubjectSelectQueryHandler(ICommandRepository<CoreSubject> repository) : IRequestHandler<CoreSubjectSelectQuery, CoreSubjectSelectEventResponse>
{
    public async Task<CoreSubjectSelectEventResponse> Handle(CoreSubjectSelectQuery request, CancellationToken cancellationToken)
    {
        var response = new CoreSubjectSelectEventResponse
        {
            Success = false
        };

        try
        {
            var coreSubjects = await repository
                .Find(cs => request.SubjectCodes.Contains(cs.SubjectCode) && cs.IsActive)
                .Select(cs => new CoreSubjectSelectEventResponseEntity
                {
                    SubjectCode = cs.SubjectCode
                })
                .Distinct()
                .ToListAsync(cancellationToken);

            response.Success = true;
            response.Response = coreSubjects;
            response.SetMessage(MessageId.I00001);
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.SetMessage(MessageId.E00000, $"Error getting core subjects: {ex.Message}");
        }

        return response;
    }
}

