using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.LearningPathCourse.Commands.UpdateLearningPathCourseStatus
{
	public class UpdateLearningPathCourseStatusHandler
	: ICommandHandler<UpdateLearningPathCourseStatusCommand, UpdateLearningPathCourseStatusResponse>
	{
		private readonly ILearningPathService _learningPathProgressService;

		public UpdateLearningPathCourseStatusHandler(ILearningPathService learningPathProgressService)
		{
			_learningPathProgressService = learningPathProgressService;
		}

		public async Task<UpdateLearningPathCourseStatusResponse> Handle(
			UpdateLearningPathCourseStatusCommand request,
			CancellationToken cancellationToken)
		{
			var response = new UpdateLearningPathCourseStatusResponse();

			await _learningPathProgressService.UpdateCourseStatusForUserAsync(
				request.UserId,
				request.CourseId,
				request.Status,
				cancellationToken);

			response.Success = true;
			response.SetMessage(MessageId.I00001, "Cập nhật trạng thái (status) khóa học trong learning path (major)");
			response.Response = "OK";

			return response;
		}
	}
}
