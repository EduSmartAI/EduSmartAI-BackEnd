using Course.Application.DTOs.SyllabusDTO;
using Course.Application.Subjects.Commands.AddSubjectToSyllabus;
using Course.Application.Syllabus.Commands.AddSemester;
using Course.Application.Syllabus.Commands.CreateSyllabus;
using Course.Application.Syllabus.Queries;

namespace Course.Infrastructure.Implements
{
	public class SyllabusService(
		IIdentityService _identityService,
		IUnitOfWork unitOfWork,
		ICommandRepository<Syllabus> _syllabusCommandRepository,
		ICommandRepository<SyllabusSemester> _syllabusSemesterCommandRepository,
		ICommandRepository<SyllabusSubject> _syllabusSubjectCommandRepository,
		ICommandRepository<Semester> _semesterCommandRepository,
		ICommandRepository<Subject> _subjectCommandRepository)
		: ISyllabusService
	{
		/// <summary>
		/// Add semester to syllabus
		/// </summary>
		/// <param name="cmd"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<AddSemesterResponse> AddSemesterAsync(AddSemesterCommand cmd, CancellationToken ct)
		{
			var response = new AddSemesterResponse { Success = false };

			var user = _identityService.GetCurrentUser()!;

			// Check existed
			var existed = await _syllabusSemesterCommandRepository.FirstOrDefaultAsync(
				x => x.SyllabusId == cmd.SyllabusId && x.SemesterId == cmd.Dto.SemesterId, ct);

			if (existed != null)
			{
				response.SetMessage(MessageId.E00000, "Học kỳ đã tồn tại trong đề cương");
				return response;
			}

			var entity = new SyllabusSemester
			{
				SyllabusId = cmd.SyllabusId,
				SemesterId = cmd.Dto.SemesterId,
				PositionIndex = cmd.Dto.PositionIndex
			};

			await _syllabusSemesterCommandRepository.AddAsync(entity);
			await unitOfWork.SaveChangesAsync(user.Email, ct);

			response.Success = true;
			response.Response = true;
			response.SetMessage(MessageId.I00001, "Thêm học kỳ vào chương trình");
			return response;
		}

		/// <summary>
		/// Add subject to syllabus semester
		/// </summary>
		/// <param name="cmd"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<AddSubjectResponse> AddSubjectAsync(AddSubjectCommand cmd, CancellationToken ct)
		{
			var response = new AddSubjectResponse { Success = false };

			// Check existed
			var existed = await _syllabusSubjectCommandRepository.FirstOrDefaultAsync(
				x => x.SyllabusId == cmd.SyllabusId &&
					 x.SemesterId == cmd.SemesterId &&
					 x.SubjectId == cmd.Dto.SubjectId, ct);

			if (existed != null)
			{
				response.SetMessage(MessageId.E00000, "Môn học đã tồn tại trong học kỳ của ");
				return response;
			}

				var entity = new SyllabusSubject
			{
				SyllabusId = cmd.SyllabusId,
				SemesterId = cmd.SemesterId,
				SubjectId = cmd.Dto.SubjectId,
				IsMandatory = cmd.Dto.IsMandatory,
				Credit = cmd.Dto.Credit,
				PositionIndex = cmd.Dto.PositionIndex
			};

			await _syllabusSubjectCommandRepository.AddAsync(entity);
			await unitOfWork.SaveChangesAsync(ct);

			response.Success = true;
			response.Response = true;
			return response;
		}

		/// <summary>
		/// Create syllabus (tên khóa)
		/// </summary>
		/// <param name="cmd"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<CreateSyllabusResponse> CreateSyllabusAsync(CreateSyllabusCommand cmd, CancellationToken ct)
		{
			var response = new CreateSyllabusResponse { Success = false };
			var user = _identityService.GetCurrentUser()!.Email;

			var existed = await _syllabusCommandRepository.FirstOrDefaultAsync(
				x => x.MajorId == cmd.MajorId && x.VersionLabel == cmd.VersionLabel, ct);

			if (existed != null)
			{
				response.SetMessage(MessageId.E00000, $"Khóa {cmd.VersionLabel} đã tồn tại");
				return response;
			}

			var entity = new Syllabus
			{
				SyllabusId = Guid.NewGuid(),
				MajorId = cmd.MajorId,
				VersionLabel = cmd.VersionLabel,
				EffectiveFrom = cmd.EffectiveFrom,
				EffectiveTo = cmd.EffectiveTo
			};

			await _syllabusCommandRepository.AddAsync(entity, user);
			await unitOfWork.SaveChangesAsync(user, ct);

			response.Success = true;
			response.Response = true;
			response.SetMessage(MessageId.I00001, $"Tạo tên khóa {cmd.VersionLabel}");
			return response;
		}

		/// <summary>
		/// Get full syllabus by version label
		/// </summary>
		/// <param name="versionLabel"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<GetFullSyllabusResponse> GetFullSyllabusAsync(string versionLabel, CancellationToken ct)
		{
			var response = new GetFullSyllabusResponse { Success = false };

			// Load syllabus
			var syllabus = await _syllabusCommandRepository.FirstOrDefaultAsync(
				x => x.VersionLabel == versionLabel, ct);

			if (syllabus == null)
			{
				response.SetMessage(MessageId.E00000, $"Không tìm thấy syllabus {versionLabel}");
				return response;
			}

			// Load semesters
			var semesters = await _syllabusSemesterCommandRepository.Find(
				x => x.SyllabusId == syllabus.SyllabusId, false, ct
			).ToListAsync(ct);

			var resultSemesters = new List<SemesterWithSubjectsDto>();

			foreach (var ss in semesters.OrderBy(x => x.PositionIndex))
			{
				var semesterMaster = await _semesterCommandRepository.FirstOrDefaultAsync(
					x => x.SemesterId == ss.SemesterId, ct);

				var subjects = await _syllabusSubjectCommandRepository.Find(
					x => x.SyllabusId == syllabus.SyllabusId && x.SemesterId == ss.SemesterId,
					false, ct).ToListAsync(ct);

				var mappedSubjects = new List<SubjectDetailDto>();

				foreach (var sb in subjects.OrderBy(x => x.PositionIndex))
				{
					var sbMaster = await _subjectCommandRepository.FirstOrDefaultAsync(
						x => x.SubjectId == sb.SubjectId, ct);

					mappedSubjects.Add(new SubjectDetailDto(
						sbMaster.SubjectId,
						sbMaster.SubjectCode,
						sbMaster.SubjectName,
						sb.Credit,
						sb.IsMandatory,
						sb.PositionIndex));
				}

				resultSemesters.Add(new SemesterWithSubjectsDto(
					ss.SemesterId,
					semesterMaster.SemesterName,
					ss.PositionIndex,
					mappedSubjects));
			}

			response.Response = new SyllabusFullDto(
				syllabus.SyllabusId,
				syllabus.MajorId,
				syllabus.VersionLabel,
				syllabus.EffectiveFrom,
				syllabus.EffectiveTo,
				resultSemesters
			);

			response.Success = true;
			return response;
		}
	}
}
