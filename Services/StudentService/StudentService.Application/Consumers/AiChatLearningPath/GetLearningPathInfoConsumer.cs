using BaseService.Common.ApiEntities;
using BuildingBlocks.Messaging.Events.AIService.AiChatLearningPathEvents;
using MassTransit;
using StudentService.Application.Applications.LearningPaths.Queries;
using StudentService.Application.Applications.LearningPaths.Queries.SelectLearningPaths;
using StudentService.Application.Interfaces;
using System.Linq;

namespace StudentService.Application.Consumers.AiChatLearningPath;

public class GetLearningPathInfoConsumer(ILearningPathService learningPathService) : IConsumer<GetLearningPathInfo>
{
    public async Task Consume(ConsumeContext<GetLearningPathInfo> context)
    {
        var request = context.Message;
        var query = new LearningPathSelectsQuery
        {
            LearningPathId = request.LearningPathId
        };

        var serviceResponse = await learningPathService.GetLearningPathById(query, request.UserId, context.CancellationToken);

        var mappedDetail = serviceResponse.Response is null
            ? new AiLearningPathDetailDto()
            : MapDetailDto(serviceResponse.Response, request.LearningPathId);

        var response = new GetLearningPathInfoResponse
        {
            Success = serviceResponse.Success,
            MessageId = serviceResponse.MessageId,
            Message = serviceResponse.Message,
            DetailErrors = CloneDetailErrors(serviceResponse.DetailErrors),
            Response = mappedDetail
        };

        await context.RespondAsync(response);
    }

    private static AiLearningPathDetailDto MapDetailDto(LearningPathSelectDto dto, Guid pathId)
    {
        return new AiLearningPathDetailDto
        {
            PathId = pathId,
            PathName = dto.PathName,
            Status = (short)dto.Status,
            CompletionPercent = dto.CompletionPercent,
            BasicCourseGroups = dto.BasicLearningPath?.CourseGroups?
                .Select(MapCourseGroup)
                .ToList() ?? new List<AiLearningPathCourseGroupDto>(),
            InternalMajors = dto.InternalLearningPath?
                .Select(MapInternalMajor)
                .ToList() ?? new List<AiLearningPathInternalMajorDto>()
        };
    }

    private static AiLearningPathInternalMajorDto MapInternalMajor(InternalLearningPathDto dto)
    {
        return new AiLearningPathInternalMajorDto
        {
            MajorId = dto.MajorId,
            MajorCode = dto.MajorCode,
            Reason = dto.Reason,
            PositionIndex = dto.PositionIndex,
            CourseGroups = dto.MajorCourseGroups?
                .Select(MapCourseGroup)
                .ToList() ?? new List<AiLearningPathCourseGroupDto>()
        };
    }

    private static AiLearningPathCourseGroupDto MapCourseGroup(CourseGroupDto group)
    {
        return new AiLearningPathCourseGroupDto
        {
            SubjectCode = group.SubjectCode,
            Status = group.Status,
            Courses = group.Courses?
                .Select(MapCourseItem)
                .ToList() ?? new List<AiLearningPathCourseItemDto>()
        };
    }

    private static AiLearningPathCourseItemDto MapCourseItem(CourseItemDto course)
    {
        return new AiLearningPathCourseItemDto
        {
            CourseId = course.CourseId,
            SubjectCode = course.SubjectCode,
            Status = course.Status,
            Title = string.IsNullOrWhiteSpace(course.Title) ? course.SubjectCode : course.Title,
            SemesterPosition = course.SemesterPosition,
            Provider = course.ShortDescription
        };
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

