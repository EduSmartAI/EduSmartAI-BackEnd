using QuizService.Application.Applications.Admin.Queries.StudentTests;
using QuizService.Application.Applications.StudentTests.Commands;
using QuizService.Application.Applications.StudentTests.Queries;

namespace QuizService.Application.Interfaces;

public interface IStudentTestService
{
    Task<StudentTestInsertResponse> InsertStudentTestAsync(StudentTestInsertCommand request, CancellationToken cancellationToken);
    
    Task<StudentTestSelectResponse> SelectStudentTestAsync(StudentTestSelectQuery request);
    
    Task<AdminStudentTestSelectDetailResponse> SelectAdminStudentTestDetailAsync(AdminStudentTestSelectDetailQuery request, CancellationToken cancellationToken);
}