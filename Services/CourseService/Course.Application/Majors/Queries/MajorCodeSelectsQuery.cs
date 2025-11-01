using BuildingBlocks.Messaging.Events.QuizService.MajorSelectsEvents;

namespace Course.Application.Majors.Queries;

public class MajorCodeSelectsQuery : IQuery<MajorSelectsEventResponse>
{
    public List<string>? MajorCodes { get; set; }
}