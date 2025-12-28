using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.PracticeTest;

public class AdminStudentSubmissionsSelectHandler(IPracticeTestService practiceTestService) 
    : IQueryHandler<StudentSubmissionsSelectRequest, StudentSubmissionsSelectResponse>
{
    public async Task<StudentSubmissionsSelectResponse> Handle(StudentSubmissionsSelectRequest request, CancellationToken cancellationToken)
    {
        return await practiceTestService.SelectAdminStudentSubmissionsAsync(request, cancellationToken);
    }
}

