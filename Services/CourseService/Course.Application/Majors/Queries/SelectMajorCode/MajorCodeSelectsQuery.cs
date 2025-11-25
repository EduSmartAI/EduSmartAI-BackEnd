using BuildingBlocks.Messaging.Events.QuizService.MajorSelectsEvents;

namespace Course.Application.Majors.Queries.SelectMajorCode;

public class MajorCodeSelectsQuery : IQuery<MajorSelectsEventResponse>
{
    public List<string>? MajorCodes { get; set; }
}