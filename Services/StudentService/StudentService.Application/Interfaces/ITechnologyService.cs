using BuildingBlocks.Messaging.Events.QuizService;
using BuildingBlocks.Messaging.Events.QuizService.TechnologySelectsEvents;
using StudentService.Application.Applications.LearningGoals.Queries;
using StudentService.Application.Applications.Technologies.Commands;
using StudentService.Application.Applications.Technologies.Queries;

namespace StudentService.Application.Interfaces;

public interface ITechnologyService
{
    Task<TechnologyInsertResponse> InsertTechnologyAsync(TechnologyInsertCommand request, CancellationToken cancellationToken);
    
    Task<TechnologyUpdateResponse> UpdateTechnologyAsync(TechnologyUpdateCommand request, CancellationToken cancellationToken);
    
    Task<TechnologyDeleteResponse> DeleteTechnologyAsync(TechnologyDeleteCommand request, CancellationToken cancellationToken);
    
    Task<TechnologySelectsEventResponse> SelectTechnologiesAsync(TechnologySelectsQuery request);
    
    Task<AdminTechnologiesSelectResponse> SelectAdminTechnologiesAsync(AdminTechnologiesSelectQuery request);
}