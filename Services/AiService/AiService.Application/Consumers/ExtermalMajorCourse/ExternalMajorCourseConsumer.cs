// using AiService.Application.Features.AiExternalCourse;
// using BaseService.Common.Utils.Const;
// using BuildingBlocks.Messaging.Events.AIService.InsertInternalExternalMajorEvent;
// using MassTransit;
// using MediatR;
//
// namespace AiService.Application.Consumers.ExternalMajorCourse
// {
//     public class ExternalMajorCourseConsumer(IMediator mediator) : IConsumer<ExternalMajorEvent>
//     {
//         public async Task Consume(ConsumeContext<ExternalMajorEvent> context)
//         {
//             var response = new ExternalMajorEventResponse { Success = false };
//             
//             var evt = context.Message;
//            
//             var responseEntities = new List<ExternalMajorEventResponseEntity>();
//             foreach (var major in evt.Majors)
//             {
//                 var request = new AiExternalCourseRequest
//                 {
//                     GoalMajor = "Tôi muốn lộ trình về mảng IT và về " + major.Description
//                 };
//                 
//                 // Call AiExternalRecommendHandler to get external courses
//                 var aiExternalCourseResponse = await mediator.Send(request);
//                 
//                 // Map response
//                 var entity = new ExternalMajorEventResponseEntity
//                 {
//                     Roadmap = aiExternalCourseResponse.Response.Roadmap == null ? null : new RoadmapPayload
//                     {
//                         RoadmapTitle = aiExternalCourseResponse.Response.Roadmap.RoadmapTitle,
//                         Steps = aiExternalCourseResponse.Response.Roadmap.Steps
//                             .Select(step => new RoadmapStepPayload
//                             {
//                                 Title = step.Title,
//                                 DurationWeeks = step.DurationWeeks,
//                                 Objectives = step.Objectives,
//                                 SuggestedCourses = step.SuggestedCourses
//                                     .Select(course => new SuggestedCoursePayload
//                                     {
//                                         Title = course.Title,
//                                         Link = course.Link,
//                                         Provider = course.Provider,
//                                         Reason = course.Reason
//                                     }).ToList()
//                             }).ToList(),
//                     }
//                 };
//                 responseEntities.Add(entity);
//             }
//             
//             // True
//             response.Success = true;
//             response.SetMessage(MessageId.I00001, "Lấy khóa học ngoài hệ thống");
//             response.Response = responseEntities;
//
//             await context.RespondAsync(response);
//         }
//     }
// }
