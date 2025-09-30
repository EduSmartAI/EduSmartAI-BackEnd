using MediatR;

namespace QuizService.Application.Applications.StudentSurveys.Queries;

public class StudentStudyTimeRequest : IRequest<StudentStudyTimeResponse>
{
    public Guid StudentId { get; set; }
}