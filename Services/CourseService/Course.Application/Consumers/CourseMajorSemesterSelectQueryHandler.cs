using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.QuizService.CourseMajorSemesterSelectEvents;

namespace Course.Application.Consumers;

public class CourseMajorSemesterSelectQueryHandler : IQueryHandler<CourseMajorSemesterSelectQuery, CourseMajorSemesterSelectEventResponse>
{
    private readonly IMajorService _majorService;
    private readonly ISemesterService _semesterService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="majorService"></param>
    /// <param name="semesterService"></param>
    public CourseMajorSemesterSelectQueryHandler(IMajorService majorService, ISemesterService semesterService)
    {
        _majorService = majorService;
        _semesterService = semesterService;
    }

    /// <summary>
    /// Handle select MajorName and SemesterName by Id
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<CourseMajorSemesterSelectEventResponse> Handle(CourseMajorSemesterSelectQuery request, CancellationToken cancellationToken)
    {
        var response = new CourseMajorSemesterSelectEventResponse {Success = false};
        
        // Get Major Name
        var majorName = await _majorService.SelectMajorAsync(request.MajorId, cancellationToken);
        if (string.IsNullOrEmpty(majorName))
        {
            response.SetMessage(MessageId.E00000, "Ngành học không tồn tại");
            return response;
        }
        
        // Get Semester Name
        var semesterName = await _semesterService.SelectSemesterAsync(request.SemesterId, cancellationToken);
        if (string.IsNullOrEmpty(semesterName))
        {
            response.SetMessage(MessageId.E00000, "Kỳ học không tồn tại");
            return response;
        }
        
        // Set Response
        response.Response = new CourseMajorSemesterSelectEventResponseEntity
        {
            MajorName = majorName,
            SemesterName = semesterName
        };
        
        // True
        response.Success = true;
        response.SetMessage(MessageId.I00001, "Lấy thông tin Ngành và Kỳ học");
        return response;
    }
}