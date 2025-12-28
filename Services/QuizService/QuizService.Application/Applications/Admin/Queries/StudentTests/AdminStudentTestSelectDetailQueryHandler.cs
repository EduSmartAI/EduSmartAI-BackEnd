using BuildingBlocks.CQRS;
using QuizService.Application.Applications.StudentTests.Queries;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.Admin.Queries.StudentTests;

public class AdminStudentTestSelectDetailQueryHandler(IStudentTestService studentTestService) : IQueryHandler<AdminStudentTestSelectDetailQuery, AdminStudentTestSelectDetailResponse>
{
    public async Task<AdminStudentTestSelectDetailResponse> Handle(AdminStudentTestSelectDetailQuery request, CancellationToken cancellationToken)
    {
        return await studentTestService.SelectAdminStudentTestDetailAsync(request, cancellationToken);
    }
}