using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.PracticeTest;

public class StudentPracticeTestSubmissionsByIdsSelectHandler(IPracticeTestService practiceTestService) : IQueryHandler<StudentPracticeTestSubmissionsByIdsSelectRequest, StudentPracticeTestSubmissionsByIdsSelectResponse>
{
    public async Task<StudentPracticeTestSubmissionsByIdsSelectResponse> Handle(StudentPracticeTestSubmissionsByIdsSelectRequest request, CancellationToken cancellationToken)
    {
        return await practiceTestService.SelectStudentPracticeTestSubmissionsByIdsAsync(request, cancellationToken);
    }
}

