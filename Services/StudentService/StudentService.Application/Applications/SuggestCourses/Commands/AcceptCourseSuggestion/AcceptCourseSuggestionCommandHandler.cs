using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentService.Application.Applications.SuggestCourses.Consumers;
using StudentService.Domain.WriteModels;
using StudentService.Domain.ReadModels;

namespace StudentService.Application.Applications.SuggestCourses.Commands.AcceptCourseSuggestion;

/// <summary>
/// Handler for accepting a course suggestion
/// </summary>
public class AcceptCourseSuggestionCommandHandler(ICommandRepository<CourseSuggestion> courseSuggestionRepository, 
                    IUnitOfWork unitOfWork,
                    IIdentityService identityService,
                    ICommandRepository<OutboxMessage> outboxMessageRepository): IRequestHandler<AcceptCourseSuggestionCommand, AcceptCourseSuggestionResponse>
{
    public async Task<AcceptCourseSuggestionResponse> Handle(AcceptCourseSuggestionCommand request, CancellationToken cancellationToken)
    {
        var response = new AcceptCourseSuggestionResponse { Success = false };
        
        // Get current user
        var currentUser =  identityService.GetCurrentUser();
        
        // Find the course suggestion in write model
        var courseSuggestion = await courseSuggestionRepository
            .Find(cs => cs.CourseSuggestionId == request.CourseSuggestionId 
                        && cs.IsActive,
                false,
                cancellationToken)
            .FirstOrDefaultAsync(cancellationToken);
        if (courseSuggestion == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy đề xuất khóa học phù hợp");
            return response;
        }

        if (courseSuggestion.StudentId != currentUser!.UserId)
        {
            response.SetMessage(MessageId.E00000, "Người dùng không có quyền chấp nhận đề xuất khóa học này");
            return response;
        }

        // Check if already accepted or rejected
        if (courseSuggestion.IsAccepted.HasValue)
        {
            response.SetMessage(MessageId.E00000, "Đề xuất khóa học đã được xử lý trước đó");
            return response;
        }

        // Update IsAccepted to true
        courseSuggestion.IsAccepted = true;

        var courseSuggestionEvent = new SuggestCourseCollectionEvent
        {
            SuggestCourseCollections = new List<CourseSuggestionCollection>
            {
                CourseSuggestionCollection.FromWriteModel(courseSuggestion)
            }
        };
        
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(SuggestCourseCollectionEvent),
            Content = System.Text.Json.JsonSerializer.Serialize(courseSuggestionEvent),
            OccurredOnUtc = DateTime.UtcNow,
        };

        await outboxMessageRepository.AddAsync(outboxMessage);

        // Update write model
        courseSuggestionRepository.Update(courseSuggestion);
        await unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken);

        // True
        response.Success = true;
        response.SetMessage(MessageId.I00001, "Chấp nhận đề xuất khóa học");
        return response;
    }
}

