using BuildingBlocks.Messaging.Events.QuizService.TechnologySelectsEvents;
using StudentService.Application.Applications.LearningGoals.Queries;
using StudentService.Application.Applications.Technologies.Commands;

namespace StudentService.Application.Interfaces;

public interface ITechnologyService
{
    Task<TechnologyInsertResponse> InsertTechnologyAsync(TechnologyInsertCommand request, CancellationToken cancellationToken);
    
    Task<TechnologySelectsEventResponse> SelectTechnologiesAsync(TechnologySelectsQuery request);
}