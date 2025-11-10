using QuizService.Application.Applications.PracticeTest;

namespace QuizService.Application.Interfaces;

public interface IPracticeTestService
{
    Task<PracticeTestAdminInsertResponse> InsertPracticeTestAsync(PracticeTestAdminInsertRequest request, CancellationToken cancellationToken);

    Task<PracticeTestAdminTestcasesInsertResponse> InsertPracticeTestTestcasesAsync(PracticeTestAdminTestcasesInsertRequest request, CancellationToken cancellationToken);
    
    Task<PracticeTestAdminTemplatesInsertResponse> InsertPracticeTestTemplatesAsync(PracticeTestAdminTemplatesInsertRequest request, CancellationToken cancellationToken);
    
    Task<PracticeTestAdminExamplesInsertResponse> InsertPracticeTestExamplesAsync(PracticeTestAdminExamplesInsertRequest request, CancellationToken cancellationToken);

    Task<PracticeTestSubmitInsertResponse> InsertPracticeTestSubmitAsync(PracticeTestSubmitInsertRequest request, CancellationToken cancellationToken);
    
    Task<PracticeTestAdminLanguageInsertResponse> InsertPracticeLanguageAsync(PracticeTestAdminLanguageInsertRequest request, CancellationToken cancellationToken);

    Task<PracticeTestSelectResponse> SelectPracticeTestAsync(PracticeTestSelectRequest request, CancellationToken cancellationToken);
    
    Task<PracticeTestSelectsResponse> SelectPracticeTestsAsync(PracticeTestSelectsRequest request, CancellationToken cancellationToken);
    
    Task<PracticeTestLanguageSelectsResponse> SelectPracticeTestLanguagesAsync(PracticeTestLanguageSelectsRequest request, CancellationToken cancellationToken);
    
    Task<PracticeTestUserTemplateCodeSelectResponse> SelectUserStubCodeAsync(PracticeTestUserTemplateCodeSelectRequest request, CancellationToken cancellationToken);
    
    Task<PracticeTestAdminUpdateResponse> UpdatePracticeTestAsync(PracticeTestAdminUpdateRequest request, CancellationToken cancellationToken);
    
    Task<PracticeTestAdminDeleteResponse> DeletePracticeTestAsync(PracticeTestAdminDeleteRequest request, CancellationToken cancellationToken); 
}