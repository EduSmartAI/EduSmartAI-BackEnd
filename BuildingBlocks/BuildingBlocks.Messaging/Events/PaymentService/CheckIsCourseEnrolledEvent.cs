using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.PaymentService
{
	public record CheckIsCourseEnrolledEvent(Guid CourseId, Guid UserId);

	public record CheckIsCourseEnrolledEventResponse : AbstractApiResponse<bool>
	{
		public override bool Response { get; set; }
	}
}
