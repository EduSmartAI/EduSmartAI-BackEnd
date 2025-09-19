using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.QuizService.SubjectSelectEvents;
using Course.Application.Interfaces;
using Course.Application.Subjects.Queries;
using Course.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Course.Infrastructure.Implements;

public class SubjectService(ICommandRepository<Subject> subjectRepository, IUnitOfWork unitOfWork) : ISubjectService
{
    
    /// <summary>
    /// Select subjects
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SubjectSelectsEventResponse> SelectSubject(SubjectSelectsQuery request, CancellationToken cancellationToken)
    {
        var response = new SubjectSelectsEventResponse {Success = false};
        
       var subjectSelects = await subjectRepository
            .Find(x => request.SubjectIds.Contains(x.SubjectId) && x.IsActive, cancellationToken: cancellationToken)
            .Select(x => new SubjectSelectEventResponseEntity
            {
                SubjectId = x.SubjectId,
                SubjectName = $"{x.SubjectName} - {x.SubjectCode}",
            }).ToListAsync(cancellationToken: cancellationToken);
        
        if (subjectSelects.Count != request.SubjectIds.Count)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy môn học phù hợp");
            return response;
        }

        // True
        response.Success = true;
        response.SetMessage(MessageId.I00001, "Lấy danh sách môn học");
        response.Response = subjectSelects;
        return response;
    }
}