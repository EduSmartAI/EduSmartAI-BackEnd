using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.QuizService;
using Microsoft.EntityFrameworkCore;

namespace Course.Application.Consumers;

public class SuggestCourseRetakeEventConsumer(ICommandRepository<CourseEntity> courseCommandRepository) : IConsumer<SuggestCourseRetakeEvent>
{
    public async Task Consume(ConsumeContext<SuggestCourseRetakeEvent> context)
    {
        var evt = context.Message;
        var response = new SuggestCourseRetakeEventResponse { Success = false };
        
        // Find the current course with Subject information and CourseRatings
        var courseExist = await courseCommandRepository
            .Find(ce => ce.CourseId == evt.CourseId && ce.IsActive,
                false,
                CancellationToken.None,
                ce => ce.Subject, 
                ce => ce.CourseRatings)
            .FirstOrDefaultAsync();

        if (courseExist == null)
        {
            response.SetMessage(MessageId.E00000, "Course not found");
            await context.RespondAsync(response);
            return;
        }

        // Get the current course level
        var currentLevel = courseExist.Level;
        
        List<SuggestCourseRetakeEventResponseEntity> courseSuggestions;
        
        if (!currentLevel.HasValue || currentLevel.Value <= 1)
        {
            // If course is at level 1 or has no level, suggest courses with same subject, same level but higher rating
            var currentAverageRating = courseExist.CourseRatings.Any() 
                ? courseExist.CourseRatings.Average(r => r.Rating) 
                : 0;
            
            // First, try to find courses with higher rating
            courseSuggestions = await courseCommandRepository
                .Find(cs => cs.SubjectId == courseExist.SubjectId 
                            && cs.Level == currentLevel 
                            && cs.IsActive 
                            && cs.CourseId != evt.CourseId,
                    false,
                    CancellationToken.None,
                    cs => cs.CourseRatings)
                .Select(x => new
                {
                    Course = x,
                    AverageRating = x!.CourseRatings.Any() ? x.CourseRatings.Average(r => r.Rating) : 0
                })
                .Where(x => x.AverageRating > currentAverageRating)
                .OrderByDescending(x => x.AverageRating)
                .Select(x => new SuggestCourseRetakeEventResponseEntity
                {
                    CourseId = x.Course!.CourseId,
                    Level = x.Course.Level,
                    Title = x.Course.Title,
                    Description = x.Course.Description,
                    DurationMinutes = x.Course.DurationMinutes,
                    CourseImageUrl = x.Course.CourseImageUrl
                })
                .ToListAsync();
            
            // If no higher-rated courses found, suggest any courses with same subject and level
            if (!courseSuggestions.Any())
            {
                courseSuggestions = await courseCommandRepository
                    .Find(cs => cs.SubjectId == courseExist.SubjectId 
                                && cs.Level == currentLevel 
                                && cs.IsActive 
                                && cs.CourseId != evt.CourseId,
                        false,
                        CancellationToken.None,
                        cs => cs.CourseRatings)
                    .Select(x => new
                    {
                        Course = x,
                        AverageRating = x!.CourseRatings.Any() ? x.CourseRatings.Average(r => r.Rating) : 0
                    })
                    .OrderByDescending(x => x.AverageRating)
                    .Select(x => new SuggestCourseRetakeEventResponseEntity
                    {
                        CourseId = x.Course!.CourseId,
                        Level = x.Course.Level,
                        Title = x.Course.Title,
                        Description = x.Course.Description,
                        DurationMinutes = x.Course.DurationMinutes,
                        CourseImageUrl = x.Course.CourseImageUrl
                    })
                    .ToListAsync();
                
                // If still no courses found at all
                if (!courseSuggestions.Any())
                {
                    response.SetMessage(MessageId.I00001, "No alternative courses available for the same subject and level");
                    await context.RespondAsync(response);
                    return;
                }
            }
        }
        else
        {
            var lowerLevel = currentLevel.Value - 1;

            // Find courses with same SubjectCode but lower level
            courseSuggestions = await courseCommandRepository
                .Find(cs => cs.SubjectId == courseExist.SubjectId 
                            && cs.Level == lowerLevel 
                            && cs.IsActive 
                            && cs.CourseId != evt.CourseId,
                    isTracking: false)
                .Select(x => new SuggestCourseRetakeEventResponseEntity
                {
                    CourseId = x!.CourseId,
                    Level = x.Level,
                    Title = x.Title,
                    Description = x.Description,
                    DurationMinutes = x.DurationMinutes,
                    CourseImageUrl = x.CourseImageUrl
                })
                .ToListAsync();
        }
        
        // True
        response.Success = true;
        response.Response = courseSuggestions;
        response.SetMessage(MessageId.I00001);
        await context.RespondAsync(response);
    }
}