using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.PracticeTest;

public class StudentPracticeTestSubmissionsSelectHandler(IPracticeTestService practiceTestService) : IQueryHandler<StudentPracticeTestSubmissionsSelectRequest, StudentPracticeTestSubmissionsSelectResponse>
{
    public async Task<StudentPracticeTestSubmissionsSelectResponse> Handle(StudentPracticeTestSubmissionsSelectRequest request, CancellationToken cancellationToken)
    {
        return await practiceTestService.SelectStudentPracticeTestSubmissionsAsync(request, cancellationToken);
    }
}

