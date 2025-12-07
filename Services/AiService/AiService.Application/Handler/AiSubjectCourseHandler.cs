using AiService.Application.Features.AiSubjectCourse;
using AiService.Application.Interfaces;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.AIService;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using static AiService.Application.Contracts.AiRecommendContracts;

namespace AiService.Application.Handler;

public class AiSubjectCourseHandler(
    IAdvisorService advisorService,
    ILogger<AiSubjectCourseHandler> logger,
    IRequestClient<GetSubjectDetailEvent> subjectDetailClient)
    : IRequestHandler<AiSubjectCourseRequest, AiSubjectCourseResponse>
{
    public async Task<AiSubjectCourseResponse> Handle(AiSubjectCourseRequest request, CancellationToken cancellationToken)
    {
        var response = new AiSubjectCourseResponse { Success = false };
        var subjectCode = NormalizeSubjectCode(request.SubjectCode);

        if (string.IsNullOrWhiteSpace(subjectCode))
        {
            response.SetMessage(MessageId.E00000, "Thiếu mã môn học.");
            return response;
        }

        var subjectTitle = request.SubjectTitle;
        var subjectDescription = request.SubjectDescription;

        try
        {
            var detailResponse = await subjectDetailClient.GetResponse<GetSubjectDetailEventResponse>(
                new GetSubjectDetailEvent { SubjectCode = subjectCode },
                cancellationToken);

            if (detailResponse.Message.Success && detailResponse.Message.Response is { } detail)
            {
                if (string.IsNullOrWhiteSpace(subjectTitle))
                {
                    subjectTitle = detail.SubjectTitle;
                }

                if (string.IsNullOrWhiteSpace(subjectDescription))
                {
                    subjectDescription = detail.SubjectDescription;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Không thể lấy thông tin môn học {SubjectCode}", subjectCode);
        }

        try
        {
            var matchRequest = new SubjectCourseMatchRequest
            {
                SubjectCode = subjectCode,
                SubjectTitle = string.IsNullOrWhiteSpace(subjectTitle) ? subjectCode : subjectTitle,
                SubjectDescription = subjectDescription ?? string.Empty,
                K = request.TopK,
                ShowSources = request.ShowSources
            };

            var result = await advisorService.MatchSubjectCoursesAsync(matchRequest, cancellationToken);

            response.Success = true;
            response.Response = result;
            response.SetMessage(MessageId.I00001, "Gợi ý khóa học thành công");
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error matching subject courses");
            response.SetMessage(MessageId.E00000, "Không thể gợi ý khóa học từ môn học");
            return response;
        }
    }

    private static string NormalizeSubjectCode(string? code) =>
        string.IsNullOrWhiteSpace(code) ? string.Empty : code.Trim().ToUpperInvariant();
}

