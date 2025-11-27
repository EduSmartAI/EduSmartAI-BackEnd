using BaseService.Common.ApiEntities;
using BuildingBlocks.Messaging.Events.AIService.AiChatLearningPathEvents;
using BuildingBlocks.Pagination;
using MassTransit;
using StudentService.Application.Applications.LearningPaths.Queries.SelectAllLearningPath;
using StudentService.Application.Interfaces;
using System.Linq;

namespace StudentService.Application.Consumers.AiChatLearningPath;

public class GetAllLearningPathConsumer(ILearningPathService learningPathService) : IConsumer<GetAllLearningPath>
{
    private const int DefaultPageSize = 20;

    public async Task Consume(ConsumeContext<GetAllLearningPath> context)
    {
        var request = context.Message;
        var query = new SelectAllLearningPathQuery(new PaginationRequest(0, DefaultPageSize));

        var serviceResponse = await learningPathService.GetAllLearningPath(query, request.UserId, context.CancellationToken);

        var summaryItems = serviceResponse.Response?.Data?
            .Select(dto => new AiLearningPathSummaryDto
            {
                PathId = dto.PathId,
                PathName = dto.PathName,
                Status = dto.Status,
                CreatedAt = dto.CreatedAt
            })
            .ToList() ?? new List<AiLearningPathSummaryDto>();

        var response = new GetAllLearningPathResponse
        {
            Success = serviceResponse.Success,
            MessageId = serviceResponse.MessageId,
            Message = serviceResponse.Message,
            DetailErrors = CloneDetailErrors(serviceResponse.DetailErrors),
            Response = summaryItems
        };

        await context.RespondAsync(response);
    }

    private static List<DetailError> CloneDetailErrors(List<DetailError>? errors)
    {
        if (errors == null || errors.Count == 0)
        {
            return new List<DetailError>();
        }

        return errors
            .Select(e => new DetailError
            {
                Field = e.Field,
                MessageId = e.MessageId,
                ErrorMessage = e.ErrorMessage
            })
            .ToList();
    }
}

