using BaseService.Application.Common;
using Course.Application.DTOs.SyllabusDTO;
using Course.Application.DTOs.SyllabusDTO.Majors;
using Course.Application.DTOs.SyllabusDTO.Semester;
using Course.Application.DTOs.SyllabusDTO.Subjects;
using Course.Application.Majors.Commands.CreateMajor;
using Course.Application.Majors.Commands.UpdateMajorDescription;
using Course.Application.Majors.Queries.GetMajorDetails;
using Course.Application.Majors.Queries.GetMajors;
using Course.Application.Semesters.Queries.GetSemesterDetails;
using Course.Application.Semesters.Queries.GetSemesters;
using Course.Application.Subjects.Commands.AddSubjectToSyllabus;
using Course.Application.Subjects.Commands.CreateSubject;
using Course.Application.Subjects.Queries.GetSubjectDetails;
using Course.Application.Subjects.Queries.GetSubjects;
using Course.Application.Syllabus.Commands.CloneCascadeSyllabus;
using Course.Application.Syllabus.Commands.CloneFoundationSyllabus;
using Course.Application.Syllabus.Commands.CreateFullSyllabus;
using Course.Application.Syllabus.Commands.CreateSyllabus;
using Course.Application.Syllabus.Commands.UpdateSyllabusSubjects;
using Course.Application.Syllabus.Queries.GetFullSyllabus;

namespace Course.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class SyllabusController(ISender sender) : ControllerBase
	{
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

		[HttpPost("[action]")]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(
			Summary = "Tạo mới chuyên ngành",
			Description = "Tạo mới chuyên ngành. Cần xác thực Bearer."
		)]
		public async Task<CreateMajorResponse> CreateMajorProcess([FromBody] CreateMajorCommand request)
		{
			return await ApiControllerHelper.HandleRequest<CreateMajorCommand, CreateMajorResponse, bool>(
				request,
				_logger,
				ModelState,
				async () => await sender.Send(request),
				new CreateMajorResponse()
			);
		}

		[HttpPost("[action]")]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(
			Summary = "Tạo mới môn học",
			Description = "Tạo mới môn học. Cần xác thực Bearer."
		)]
		public async Task<CreateSubjectResponse> CreateSubjectProcess([FromBody] CreateSubjectCommand request)
		{
			return await ApiControllerHelper.HandleRequest<CreateSubjectCommand, CreateSubjectResponse, bool>(
				request,
				_logger,
				ModelState,
				async () => await sender.Send(request),
				new CreateSubjectResponse()
			);
		}

		[HttpPost("[action]")]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(
			Summary = "Tạo mới chương trình đào tạo - FE không dùng API này",
			Description = "Tạo mới chương trình đào tạo. Cần xác thực Bearer."
		)]
		public async Task<CreateSyllabusResponse> CreateSyllabus([FromBody] CreateSyllabusCommand cmd)
		=> await ApiControllerHelper.HandleRequest<CreateSyllabusCommand, CreateSyllabusResponse, bool>(
			cmd,
			_logger,
			ModelState,
			() => sender.Send(cmd),
			new());


		[HttpPost("[action]")]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(
			Summary = "Tạo mới chương trình đào tạo cho chuyên ngành",
			Description = "Tạo mới chương trình đào tạo cho chuyên ngành. Cần xác thực Bearer."
		)]
		public async Task<CreateFullSyllabusResponse> CreateFullSyllabusForMajor([FromBody] CreateFullSyllabusCommand cmd)
		{
			return await ApiControllerHelper.HandleRequest<CreateFullSyllabusCommand, CreateFullSyllabusResponse, bool>(
				cmd,
				_logger,
				ModelState,
				() => sender.Send(cmd),
				new()
			);
		}


		[HttpPost("{syllabusId:guid}/semesters/{semesterId:guid}/subjects")]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(
			Summary = "Thêm môn học vào học kỳ của chương trình đào tạo - FE không dùng API này",
			Description = "Thêm môn học vào học kỳ của chương trình đào tạo. Cần xác thực Bearer."
		)]
		public async Task<AddSubjectResponse> AddSubjectToSyllabusSemester([FromRoute] Guid syllabusId, [FromRoute] Guid semesterId, [FromBody] AddSubjectToSyllabusDto dto)
		{
			return await ApiControllerHelper.HandleRequest<AddSubjectCommand, AddSubjectResponse, bool>(
				new AddSubjectCommand(syllabusId, semesterId, dto),
				_logger,
				ModelState,
				() => sender.Send(new AddSubjectCommand(syllabusId, semesterId, dto)),
				new()
			);
		}

		[HttpGet("full/{versionLabel}/{majorCode}")]
		[SwaggerOperation(
			Summary = "Lấy đầy đủ thông tin chương trình đào tạo theo phiên bản",
			Description = "Lấy đầy đủ thông tin chương trình đào tạo theo phiên bản."
		)]
		public async Task<GetFullSyllabusResponse> GetFullSyllabus([FromRoute] string versionLabel, [FromRoute] string majorCode)
		{
			var query = new GetFullSyllabusQuery(versionLabel, majorCode);
			return await ApiControllerHelper.HandleRequest<GetFullSyllabusQuery, GetFullSyllabusResponse, SyllabusFullDto>(
				query,
				_logger,
				ModelState,
				() => sender.Send(query),
				new()
			);
		}

		[HttpPost("clone/cascade")]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(
			Summary = "Clone chương trình đào tạo nền tảng kèm theo các môn học",
			Description = "Clone chương trình đào tạo nền tảng kèm theo các môn học. Cần xác thực Bearer."
		)]
		public async Task<CloneCascadeSyllabusResponse> CloneCascadeSyllabus([FromBody] CloneCascadeSyllabusCommand cmd)
		{
			return await ApiControllerHelper.HandleRequest<CloneCascadeSyllabusCommand, CloneCascadeSyllabusResponse, bool>(
				cmd,
				_logger,
				ModelState,
				() => sender.Send(cmd),
				new()
			);
		}

		[HttpPost("clone/foundation")]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(
			Summary = "Clone chương trình đào tạo nền tảng",
			Description = "Clone chương trình đào tạo nền tảng. Cần xác thực Bearer."
		)]
		public async Task<CloneFoundationSyllabusResponse> CloneFoundationOnlySyllabus([FromBody] CloneFoundationSyllabusCommand cmd)
		{
			return await ApiControllerHelper.HandleRequest<CloneFoundationSyllabusCommand, CloneFoundationSyllabusResponse, bool>(
				cmd,
				_logger,
				ModelState,
				() => sender.Send(cmd),
				new()
			);
		}

		[HttpGet("[action]")]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(Summary = "Get list of semesters")]
		public async Task<GetSemestersResponse> GetSemesters([FromQuery] int? page, [FromQuery] int? size, [FromQuery] string? search)
		{
			var request = new GetSemestersQuery(page, size, search);

			return await ApiControllerHelper.HandleRequest<GetSemestersQuery, GetSemestersResponse, PagedResult<SemesterDto>>(
				request,
				_logger,
				ModelState,
				async () => await sender.Send(request),
				new GetSemestersResponse()
			);
		}

		[HttpGet("[action]/{semesterId:guid}")]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(Summary = "Get semester detail by id")]
		public async Task<GetSemesterDetailResponse> GetSemesterDetail([FromRoute] Guid semesterId)
		{
			var request = new GetSemesterDetailQuery(semesterId);
			return await ApiControllerHelper.HandleRequest<GetSemesterDetailQuery, GetSemesterDetailResponse, SemesterDto>(
				request,
				_logger,
				ModelState,
				async () => await sender.Send(request),
				new GetSemesterDetailResponse()
			);
		}

		[HttpGet("[action]")]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(Summary = "Get list of majors")]
		public async Task<GetMajorsResponse> GetMajors(
			[FromQuery] int? page,
			[FromQuery] int? size,
			[FromQuery] string? search)
		{
			var request = new GetMajorsQuery(page, size, search);

			return await ApiControllerHelper.HandleRequest<GetMajorsQuery, GetMajorsResponse, PagedResult<MajorDto>>(
				request,
				_logger,
				ModelState,
				async () => await sender.Send(request),
				new GetMajorsResponse()
			);
		}

		[HttpGet("[action]/{majorId:guid}")]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(Summary = "Get major detail by id")]
		public async Task<GetMajorDetailResponse> GetMajorDetail([FromRoute] Guid majorId)
		{
			var request = new GetMajorDetailQuery(majorId);

			return await ApiControllerHelper.HandleRequest<GetMajorDetailQuery, GetMajorDetailResponse, MajorDto?>(
				request,
				_logger,
				ModelState,
				async () => await sender.Send(request),
				new GetMajorDetailResponse()
			);
		}

		[HttpGet("[action]")]
		[SwaggerOperation(Summary = "Get list of subjects")]
		public async Task<GetSubjectsResponse> GetSubjects(
			[FromQuery] int? page,
			[FromQuery] int? size,
			[FromQuery] string? search)
		{
			var request = new GetSubjectsQuery(page, size, search);

			return await ApiControllerHelper.HandleRequest<
				GetSubjectsQuery,
				GetSubjectsResponse,
				PagedResult<SubjectDto>>(
				request,
				_logger,
				ModelState,
				async () => await sender.Send(request),
				new GetSubjectsResponse()
			);
		}

		[HttpGet("[action]/{subjectId:guid}")]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(Summary = "Get subject detail by id")]
		public async Task<GetSubjectDetailResponse> GetSubjectDetail([FromRoute] Guid subjectId)
		{
			var request = new GetSubjectDetailQuery(subjectId);

			return await ApiControllerHelper.HandleRequest<
				GetSubjectDetailQuery,
				GetSubjectDetailResponse,
				SubjectDto?>(
				request,
				_logger,
				ModelState,
				async () => await sender.Send(request),
				new GetSubjectDetailResponse()
			);
		}

		[HttpPut]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(
			Summary = "Cập nhật môn học cho chương trình đào tạo",
			Description = "Cập nhật môn học cho chương trình đào tạo. Cần xác thực Bearer."
		)]
		public async Task<UpdateSyllabusSubjectsResponse> UpdateSyllabusSubjects([FromBody] UpdateSyllabusSubjectsCommand request)
		{
			return await ApiControllerHelper.HandleRequest<UpdateSyllabusSubjectsCommand, UpdateSyllabusSubjectsResponse, bool>(
				request,
				_logger,
				ModelState,
				async () => await sender.Send(request),
				new UpdateSyllabusSubjectsResponse()
			);
		}

		[HttpPut("major/{id}")]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(
			Summary = "Cập nhật mô tả chuyên ngành",
			Description = "Cập nhật mô tả chuyên ngành. Cần xác thực Bearer."
		)]
		public async Task<UpdateMajorDescriptionResponse> UpdateMajorDescription([FromRoute] Guid id, [FromBody] string description)
		{
			var command = new UpdateMajorDescriptionCommand(id, description);
			return await ApiControllerHelper.HandleRequest<
				UpdateMajorDescriptionCommand,
				UpdateMajorDescriptionResponse,
				bool>(
				command,
				_logger,
				ModelState,
				async () => await sender.Send(command),
				new UpdateMajorDescriptionResponse()
			);
		}
	}
}
