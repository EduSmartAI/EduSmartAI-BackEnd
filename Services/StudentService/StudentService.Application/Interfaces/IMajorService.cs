using StudentService.Application.Applications.Majors.Commands;
using StudentService.Application.Applications.Majors.Queries;
using StudentService.Domain.WriteModels;

namespace StudentService.Application.Interfaces;

public interface IMajorService
{ 
    Task<MajorInsertResponse> InsertMajorAsync(MajorInsertCommand request, CancellationToken cancellationToken);
    Task<MajorsSelectResponse> SelectMajorsAsync(MajorsSelectQuery request);
}