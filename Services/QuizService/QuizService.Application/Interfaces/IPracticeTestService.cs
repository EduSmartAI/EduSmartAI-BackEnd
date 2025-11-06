using QuizService.Application.Applications.PracticeTest;

namespace QuizService.Application.Interfaces;

public interface IPracticeTestService
{
    Task<PracticeTestSelectResponse> SelectPracticeTestAsync(PracticeTestSelectRequest request, CancellationToken cancellationToken);
    
    Task<PracticeTestSelectsResponse> SelectPracticeTestsAsync(PracticeTestSelectsRequest request, CancellationToken cancellationToken);
    
    Task<PracticeTestLanguageSelectsResponse> SelectPracticeTestLanguagesAsync(PracticeTestLanguageSelectsRequest request, CancellationToken cancellationToken);
    
    Task<PracticeTestSubmitInsertResponse> InsertPracticeTestSubmitAsync(PracticeTestSubmitInsertRequest request, CancellationToken cancellationToken);
}