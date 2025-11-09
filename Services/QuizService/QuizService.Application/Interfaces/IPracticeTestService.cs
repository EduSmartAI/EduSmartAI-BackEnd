using QuizService.Application.Applications.PracticeTest;

namespace QuizService.Application.Interfaces;

public interface IPracticeTestService
{
    Task<PracticeTestAdminInsertResponse> InsertPracticeTestAsync(PracticeTestAdminInsertRequest request, CancellationToken cancellationToken);

    Task<PracticeTestTestcasesInsertResponse> InsertPracticeTestTestcasesAsync(PracticeTestTestcasesInsertRequest request, CancellationToken cancellationToken);
    
    Task<PracticeTestTemplatesResponse> InsertPracticeTestTemplatesAsync(PracticeTestTemplatesInsertRequest request, CancellationToken cancellationToken);
    
    Task<PracticeTestExamplesInsertResponse> InsertPracticeTestExamplesAsync(PracticeTestExamplesInsertRequest request, CancellationToken cancellationToken);

    Task<PracticeTestSubmitInsertResponse> InsertPracticeTestSubmitAsync(PracticeTestSubmitInsertRequest request, CancellationToken cancellationToken);

    Task<PracticeTestSelectResponse> SelectPracticeTestAsync(PracticeTestSelectRequest request, CancellationToken cancellationToken);
    
    Task<PracticeTestSelectsResponse> SelectPracticeTestsAsync(PracticeTestSelectsRequest request, CancellationToken cancellationToken);
    
    Task<PracticeTestLanguageSelectsResponse> SelectPracticeTestLanguagesAsync(PracticeTestLanguageSelectsRequest request, CancellationToken cancellationToken);
    
    Task<PracticeTestUserTemplateCodeSelectResponse> SelectUserStubCodeAsync(PracticeTestUserTemplateCodeSelectRequest request, CancellationToken cancellationToken);
    
    Task<PracticeTestAdminUpdateResponse> UpdatePracticeTestAsync(PracticeTestAdminUpdateRequest request, CancellationToken cancellationToken);
    
    Task<PracticeTestAdminDeleteResponse> DeletePracticeTestAsync(PracticeTestAdminDeleteRequest request, CancellationToken cancellationToken);
}