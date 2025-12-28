using AiService.Application.Features.AiRecommend;
using BuildingBlocks.Messaging.Events.AIService.AiRecommend;
using MassTransit;
using MediatR;
using DependentSubjectWarning = BuildingBlocks.Messaging.Events.AIService.AiRecommend.DependentSubjectWarning;

namespace AiService.Application.Consumers.AiRecommend;

public class SubjectMarkUpdateEventConsumer(IMediator mediator) : IConsumer<SubjectMarkUpdateEvent>
{
    public async Task Consume(ConsumeContext<SubjectMarkUpdateEvent> context)
    {
        var message = context.Message;
        var results = new List<SubjectMarkUpdateAnalysisDto>();
        
        // Xử lý từng subject mark và thu thập kết quả
        var tasks = message.SubjectMarks.Select(async subjectMark =>
        {
            var request = new SubjectMarkUpdateRequest
            {
                SubjectCode = subjectMark.SubjectCode,
                SubjectName = subjectMark.SubjectName,
                OldMark = subjectMark.OldMark,
                NewMark = subjectMark.NewMark,
                NewAnalysis = subjectMark.NewAnalysis,
                CareerGoal = subjectMark.CareerGoal
            };
            
            // Gọi handler qua mediator
            var response = await mediator.Send(request, context.CancellationToken);
            
            // Chuyển đổi response thành DTO
            if (response.Success && response.Response != null)
            {
                return new SubjectMarkUpdateAnalysisDto
                {
                    SubjectCode = response.Response.SubjectCode,
                    SubjectName = response.Response.SubjectName,
                    OldMark = response.Response.OldMark > 0 ? (double?)response.Response.OldMark : null,
                    NewMark = response.Response.NewMark,
                    MarkImprovement = response.Response.MarkImprovement,
                    ImprovementAnalysis = response.Response.ImprovementAnalysis,
                    ComparisonAnalysis = response.Response.ComparisonAnalysis,
                    DependentWarnings = response.Response.DependentWarnings?.Select(w => new DependentSubjectWarning
                    {
                        SubjectCode = w.SubjectCode,
                        SubjectName = w.SubjectName,
                        SemesterIndex = w.SemesterIndex,
                        WarningMessage = w.WarningMessage
                    }).ToList() ?? new List<DependentSubjectWarning>()
                };
            }
            
            return null;
        });
        
        // Chờ tất cả các task hoàn thành và thu thập kết quả
        var completedResults = await Task.WhenAll(tasks);
        results.AddRange(completedResults.Where(r => r != null)!);
        
        // Trả về response
        var responseEvent = new SubjectMarkUpdateEventResponse
        {
            Success = true,
            Response = results
        };
        
        await context.RespondAsync(responseEvent);
    }
}

