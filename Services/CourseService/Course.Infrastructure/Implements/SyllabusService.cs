using Course.Application.DTOs.SyllabusDTO;
using Course.Application.Subjects.Commands.AddSubjectToSyllabus;
using Course.Application.Syllabus.Commands.CloneCascadeSyllabus;
using Course.Application.Syllabus.Commands.CloneFoundationSyllabus;
using Course.Application.Syllabus.Commands.CreateFullSyllabus;
using Course.Application.Syllabus.Commands.CreateSyllabus;
using Course.Application.Syllabus.Queries.GetFullSyllabus;

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

			var entitySyllabusSemester = new SyllabusSemester
			{
				SyllabusId = cmd.SyllabusId,
				SemesterId = cmd.SemesterId,
				PositionIndex = cmd.Dto.PositionIndex
			};

			var entitySyllabusSubject = new SyllabusSubject
			{
				SyllabusId = cmd.SyllabusId,
				SemesterId = cmd.SemesterId,
				SubjectId = cmd.Dto.SubjectId,
				IsMandatory = cmd.Dto.IsMandatory,
				Credit = cmd.Dto.Credit,
				PositionIndex = cmd.Dto.PositionIndex
			};

			await unitOfWork.BeginTransactionAsync(async () =>
			{
				await _syllabusSemesterCommandRepository.AddAsync(entitySyllabusSemester);
				await _syllabusSubjectCommandRepository.AddAsync(entitySyllabusSubject);

				await unitOfWork.SaveChangesAsync(ct);

				return true;
			}, ct);

			response.Success = true;
			response.Response = true;
			return response;
		}

		/// <summary>
		/// Clone cascade syllabus
		/// </summary>
		/// <param name="dto"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<CloneCascadeSyllabusResponse> CloneCascadeAsync(CloneCascadeSyllabusDto dto, CancellationToken ct)
		{
			var response = new CloneCascadeSyllabusResponse { Success = false };
			var email = _identityService.GetCurrentUser()!.Email;

			// 1) Lấy syllabus gốc (base version)
			var baseSl = await _syllabusCommandRepository.FirstOrDefaultAsync(
				x => x.VersionLabel == dto.BaseVersion &&
					 x.Major.MajorCode == dto.MajorCode, ct);

			if (baseSl == null)
			{
				// fallback về K19
				baseSl = await _syllabusCommandRepository.FirstOrDefaultAsync(
					x => x.VersionLabel == "K19" &&
						 x.Major.MajorCode == dto.MajorCode, ct);

				if (baseSl == null)
				{
					response.SetMessage(MessageId.E00000, "Không tìm thấy syllabus để clone.");
					return response;
				}
			}

			// 2) Lấy SE foundation của phiên bản tương ứng
			var seFoundation = await _syllabusCommandRepository.FirstOrDefaultAsync(
				x => x.VersionLabel == baseSl.VersionLabel &&
					 x.Major.MajorCode == "SE", ct);

			if (seFoundation == null)
			{
				seFoundation = await _syllabusCommandRepository.FirstOrDefaultAsync(
					x => x.VersionLabel == "K19" &&
						 x.Major.MajorCode == "SE", ct);
			}

			var newSyllabusId = Guid.NewGuid();
			var newSyllabus = new Syllabus
			{
				SyllabusId = newSyllabusId,
				MajorId = baseSl.MajorId,
				VersionLabel = dto.NewVersion,
				EffectiveFrom = dto.EffectiveFrom,
				EffectiveTo = dto.EffectiveTo
			};

			await unitOfWork.BeginTransactionAsync(async () =>
			{
				// Save new syllabus
				await _syllabusCommandRepository.AddAsync(newSyllabus, email);

				//// Copy foundation S1–S4
				//var foundationSem = await _syllabusSemesterCommandRepository
				//	.Find(x => x.SyllabusId == seFoundation.SyllabusId, false, ct, x => x.Semester)
				//	.ToListAsync(ct)

				//foreach (var s in foundationSem.Where(s => s.Semester.SemesterNumber <= 4))
				//{/
				//	await CloneSemesterAndSubjects(seFoundation.SyllabusId, newSyllabusId, s.SemesterId, ct)
				//}/

				// Copy specialized S5–S9 (only is_active subjects)
				var childSemesters = await _syllabusSemesterCommandRepository
												.Find(x => x.SyllabusId == baseSl.SyllabusId, false, ct, x => x.Semester)
												.ToListAsync(ct);


				foreach (var s in childSemesters.Where(s => s.Semester.SemesterNumber >= 5))
				{
					await CloneSemesterAndSubjects(baseSl.SyllabusId, newSyllabusId, s.SemesterId, ct, onlyActive: true);
				}

				await unitOfWork.SaveChangesAsync(ct);
				return true;
			}, ct);

			response.Success = true;
			response.Response = true;
			response.SetMessage(MessageId.I00001, $"Clone syllabus cho chuyên ngành {dto.MajorCode} của khóa {dto.NewVersion} từ khóa {dto.BaseVersion}");
			return response;
		}

		/// <summary>
		/// Clone foundation syllabus
		/// </summary>
		/// <param name="dto"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<CloneFoundationSyllabusResponse> CloneFoundationAsync(CloneFoundationSyllabusDto dto, CancellationToken ct)
		{
			var response = new CloneFoundationSyllabusResponse { Success = false };
			var email = _identityService.GetCurrentUser()!.Email;

			// 1) Lấy SE foundation version
			var seSl = await _syllabusCommandRepository.FirstOrDefaultAsync(
				x => x.VersionLabel == dto.BaseVersion &&
					 x.Major.MajorCode == "SE", ct);

			if (seSl == null)
			{
				seSl = await _syllabusCommandRepository.FirstOrDefaultAsync(
					x => x.VersionLabel == "K19" &&
						 x.Major.MajorCode == "SE", ct);
			}

			var newSyllabusId = Guid.NewGuid();
			var newSyllabus = new Syllabus
			{
				SyllabusId = newSyllabusId,
				MajorId = seSl.MajorId,
				VersionLabel = dto.NewVersion,
				EffectiveFrom = dto.EffectiveFrom,
				EffectiveTo = dto.EffectiveTo
			};

			await unitOfWork.BeginTransactionAsync(async () =>
			{
				// Create new syllabus
				await _syllabusCommandRepository.AddAsync(newSyllabus, email);

				// Copy foundation only (S1–S4)
				var foundationSem = await _syllabusSemesterCommandRepository
												.Find(x => x.SyllabusId == seSl.SyllabusId, false, ct,
													x => x.Semester) 
												.ToListAsync(ct);


				foreach (var s in foundationSem.Where(s => s.Semester.SemesterNumber <= 4))
				{
					await CloneSemesterAndSubjects(seSl.SyllabusId, newSyllabusId, s.SemesterId, ct);
				}

				await unitOfWork.SaveChangesAsync(ct);
				return true;
			}, ct);

			response.Success = true;
			response.Response = true;
			response.SetMessage(MessageId.I00001, $"Clone foundation syllabus cho khoá {dto.NewVersion} từ khóa {dto.BaseVersion}");
			return response;
		}

		public async Task<CreateFullSyllabusResponse> CreateFullSyllabusAsync(CreateFullSyllabusDto dto, CancellationToken ct)
		{
			var response = new CreateFullSyllabusResponse { Success = false };
			var email = _identityService.GetCurrentUser()!.Email;

			// 1. Check version existed
			var existed = await _syllabusCommandRepository.FirstOrDefaultAsync(
				x => x.MajorId == dto.MajorId && x.VersionLabel == dto.VersionLabel, ct);

			if (existed != null)
			{
				response.SetMessage(MessageId.E00000, "Syllabus version already exists");
				return response;
			}

			// 2. Create Syllabus entity
			var syllabusId = Guid.NewGuid();
			var syllabus = new Syllabus
			{
				SyllabusId = syllabusId,
				MajorId = dto.MajorId,
				VersionLabel = dto.VersionLabel,
				EffectiveFrom = dto.EffectiveFrom,
				EffectiveTo = dto.EffectiveTo
			};

			await unitOfWork.BeginTransactionAsync(async () =>
			{
				await _syllabusCommandRepository.AddAsync(syllabus, email);

				// 3. Insert all semesters
				foreach (var sem in dto.Semesters)
				{
					var slsSem = new SyllabusSemester
					{
						SyllabusId = syllabusId,
						SemesterId = sem.SemesterId,
						PositionIndex = sem.PositionIndex
					};

					await _syllabusSemesterCommandRepository.AddAsync(slsSem);

					// 4. Insert all subjects inside this semester
					foreach (var sub in sem.Subjects)
					{
						var slsSub = new SyllabusSubject
						{
							SyllabusId = syllabusId,
							SemesterId = sem.SemesterId,
							SubjectId = sub.SubjectId,
							Credit = sub.Credit,
							IsMandatory = sub.IsMandatory,
							PositionIndex = sub.PositionIndex
						};

						await _syllabusSubjectCommandRepository.AddAsync(slsSub);
					}
				}

				await unitOfWork.SaveChangesAsync(ct);
				return true;
			}, ct);

			response.Success = true;
			response.Response = true;
			response.SetMessage(MessageId.I00001, "Tạo syllabus");
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


		#region Private Helpers Methods

		/// <summary>
		/// Clone semester and its subjects from old syllabus to new syllabus
		/// </summary>
		/// <param name="oldSyllabusId"></param>
		/// <param name="newSyllabusId"></param>
		/// <param name="semesterId"></param>
		/// <param name="ct"></param>
		/// <param name="onlyActive"></param>
		/// <returns></returns>
		private async Task CloneSemesterAndSubjects(
			Guid oldSyllabusId,
			Guid newSyllabusId,
			Guid semesterId,
			CancellationToken ct,
			bool onlyActive = false)
		{
			// ----- 1. Clone semester entry -----
			var semester = await _semesterCommandRepository.FirstOrDefaultAsync(
				x => x.SemesterId == semesterId, ct);

			var newSem = new SyllabusSemester
			{
				SyllabusId = newSyllabusId,
				SemesterId = semesterId,
				PositionIndex = semester.SemesterNumber
			};

			await _syllabusSemesterCommandRepository.AddAsync(newSem);


			// ----- 2. Load subjects from old syllabus -----
			var rawSubjects = await _syllabusSubjectCommandRepository
				.Find(x => x.SyllabusId == oldSyllabusId && x.SemesterId == semesterId, false, ct)
				.ToListAsync(ct);


			// ----- 3. Filter only active subjects if needed -----
			var subjects = new List<SyllabusSubject>();

			foreach (var sb in rawSubjects)
			{
				var subjectEntity = await _subjectCommandRepository.FirstOrDefaultAsync(
					x => x.SubjectId == sb.SubjectId, ct);

				if (onlyActive && (subjectEntity == null || !subjectEntity.IsActive))
					continue;

				subjects.Add(sb);
			}


			// ----- 4. Sort subjects by original PositionIndex (FPT cách làm chuẩn) -----
			subjects = subjects
				.OrderBy(x => x.PositionIndex)
				.ThenBy(x => x.SubjectId) // tránh tie-break
				.ToList();


			// ----- 5. Reset PositionIndex = 1,2,3,4… for this semester -----
			int newIndex = 1;

			foreach (var sb in subjects)
			{
				var newSb = new SyllabusSubject
				{
					SyllabusId = newSyllabusId,
					SemesterId = semesterId,
					SubjectId = sb.SubjectId,
					Credit = sb.Credit,
					IsMandatory = sb.IsMandatory,
					PositionIndex = newIndex++        // ← Reset index here
				};

				await _syllabusSubjectCommandRepository.AddAsync(newSb);
			}
		}


		#endregion
	}
}
