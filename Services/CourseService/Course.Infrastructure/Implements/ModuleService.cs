using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using Course.Application.Courses.Commands.UpdateModule;
using Course.Application.DTOs.CoursesDTO;
using Course.Application.DTOs.LessonsDTO;
using Course.Application.DTOs.ModulesDTO;
using Course.Application.Interfaces;
using Course.Domain.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Course.Infrastructure.Implements
{
	public class ModuleService(
		ICommandRepository<Module> _moduleRepository,
		IUnitOfWork unitOfWork,
		IIdentityService _identityService) : IModuleService
	{
		/// <summary>
		/// Update Module with its Objectives and Lessons
		/// </summary>
		/// <param name="moduleId"></param>
		/// <param name="dto"></param>
		/// <param name="ct"></param>
		/// <returns></returns>

		/*
		 | Thực thể | Điều kiện payload  |                  Tồn tại trong DB | Hành động    |
		 | -------- | ------------------ | --------------------------------: | ------------ |
		 | Module   | `moduleId == null` |                                 — | **Thêm mới** |
		 | Module   | `moduleId != null` |                   Có trong course | **Cập nhật** |
		 | Module   | (Bất kỳ)           | Không còn xuất hiện trong payload | **Xóa**      |
		 | Lesson   | `lessonId == null` |                                 — | **Thêm mới** |
		 | Lesson   | `lessonId != null` |                   Có trong module | **Cập nhật** |
		 | Lesson   | (Bất kỳ)           | Không còn xuất hiện trong payload | **Xóa**      |
		 */
		public async Task<UpdateModuleResponse> UpdateModuleAsync(Guid moduleId, UpdateModuleDto dto, CancellationToken ct = default)
		{
			var response = new UpdateModuleResponse { Success = false };

			var currentUser = _identityService.GetCurrentUser();

			if (currentUser is null)
			{
				currentUser = new IdentityEntity
				{
					UserId = Guid.Empty,
					FullName = "system",
					Email = "system",
				};
			}

			// 1. Validate PositionIndex uniqueness
			ValidateModulePositionIndexes(dto);

			// 2. Get existing module with all related data
			var existingModule = await _moduleRepository
				.Find(x => x.ModuleId == moduleId, isTracking: true, ct,
					x => x.ModuleObjectives,
					x => x.Lessons,
					x => x.ModuleDiscussions,
					x => x.ModuleMaterials)
				.FirstOrDefaultAsync(ct);

			if (existingModule is null)
			{
				response.Message = $"Module {moduleId} not found";
				return response;
			}

			// 3. Update basic module properties
			existingModule.ModuleName = dto.ModuleName?.Trim() ?? string.Empty;
			existingModule.Description = dto.Description;
			existingModule.PositionIndex = dto.PositionIndex;
			existingModule.IsActive = dto.IsActive;
			existingModule.IsCore = dto.IsCore;
			existingModule.DurationMinutes = dto.DurationMinutes;
			existingModule.Level = dto.Level;

			// 4. Update ModuleObjectives
			await UpdateModuleObjectivesAsync(existingModule, dto.Objectives, currentUser.Email);

			// 5. Update Lessons
			await UpdateLessonsAsync(existingModule, dto.Lessons, currentUser.Email);

			// 5.1 Update ModuleDiscussions
			await UpdateModuleDiscussionInternalAsync(existingModule, dto.Discussions, currentUser.Email);

			// 5.2 Update ModuleMaterials
			await UpdateModuleMaterialsInternalAsync(existingModule, dto.Materials, currentUser.Email);

			// 6. Save changes in transaction
			await unitOfWork.BeginTransactionAsync(async () =>
			{
				_moduleRepository.Update(existingModule, currentUser.Email);
				await unitOfWork.SaveChangesAsync(ct);
				return true;
			}, ct);

			// 7. Return success response
			response.Success = true;
			response.Message = "Module updated successfully";

			return response;
		}

		private Task UpdateModuleObjectivesAsync(Module module, List<UpdateModuleObjectiveDto>? objectives, string currentUser)
		{
			if (objectives is null || objectives.Count == 0)
			{
				// Mark all existing objectives as inactive (soft delete)
				foreach (var obj in module.ModuleObjectives.Where(o => o.IsActive))
				{
					obj.IsActive = false;
				}
				return Task.CompletedTask;
			}

			var now = DateTime.UtcNow;
			var existingObjectives = module.ModuleObjectives.ToDictionary(o => o.ObjectiveId, o => o);
			var payloadObjectiveIds = objectives.Where(o => o.ObjectiveId.HasValue).Select(o => o.ObjectiveId!.Value).ToHashSet();

			// 1. Mark objectives not in payload as inactive (soft delete)
			foreach (var existing in existingObjectives.Values.Where(o => o.IsActive && !payloadObjectiveIds.Contains(o.ObjectiveId)))
			{
				existing.IsActive = false;
			}

			// 2. Update existing objectives or create new ones
			foreach (var objDto in objectives)
			{
				if (objDto.ObjectiveId.HasValue && existingObjectives.TryGetValue(objDto.ObjectiveId.Value, out var existing))
				{
					// Update existing objective
					existing.Content = objDto.Content;
					existing.PositionIndex = objDto.PositionIndex;
					existing.IsActive = objDto.IsActive;
				}
				else
				{
					// Create new objective - let EF generate the ID
					var newObjective = new ModuleObjective
					{
						ModuleId = module.ModuleId,
						Content = objDto.Content,
						PositionIndex = objDto.PositionIndex,
						IsActive = objDto.IsActive,
						CreatedAt = now,
						UpdatedAt = now,
						CreatedBy = currentUser,
						UpdatedBy = currentUser
					};
					module.ModuleObjectives.Add(newObjective);
				}
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Update ModuleDiscussions for existing module
		/// </summary>
		/// <param name="module"></param>
		/// <param name="discussions"></param>
		/// <param name="actor"></param>
		/// <returns></returns>
		private Task UpdateModuleDiscussionInternalAsync(Module module, List<UpdateModuleDiscussionDto>? discussions, string actor)
		{
			if (discussions is null || discussions.Count == 0)
			{
				// Mark all existing discussions as inactive (soft delete)
				foreach (var dis in module.ModuleDiscussions.Where(d => d.IsActive))
				{
					dis.IsActive = false;
				}
				return Task.CompletedTask;
			}
			var now = DateTime.UtcNow;
			var existingDiscussions = module.ModuleDiscussions.ToDictionary(d => d.DiscussionId, d => d);
			var payloadDiscussionIds = discussions.Where(d => d.DiscussionId.HasValue).Select(d => d.DiscussionId!.Value).ToHashSet();
			// 1. Mark discussions not in payload as inactive (soft delete)
			foreach (var existing in existingDiscussions.Values.Where(d => d.IsActive && !payloadDiscussionIds.Contains(d.DiscussionId)))
			{
				existing.IsActive = false;
			}
			// 2. Update existing discussions or create new ones
			foreach (var disDto in discussions)
			{
				if (disDto.DiscussionId.HasValue && existingDiscussions.TryGetValue(disDto.DiscussionId.Value, out var existing))
				{
					// Update existing discussion
					existing.Title = disDto.Title;
					existing.Description = disDto.Description;
					existing.DiscussionQuestion = disDto.DiscussionQuestion;
					existing.IsActive = disDto.IsActive;
				}
				else
				{
					// Create new discussion - let EF generate the ID
					var newDiscussion = new ModuleDiscussion
					{
						ModuleId = module.ModuleId,
						Title = disDto.Title,
						Description = disDto.Description,
						DiscussionQuestion = disDto.DiscussionQuestion,
						IsActive = disDto.IsActive,
						CreatedAt = now,
						UpdatedAt = now,
						CreatedBy = actor,
						UpdatedBy = actor
					};
					module.ModuleDiscussions.Add(newDiscussion);
				}
			}
			return Task.CompletedTask;
		}

		/// <summary>
		/// Update ModuleMaterials for existing module
		/// </summary>
		/// <param name="module"></param>
		/// <param name="materials"></param>
		/// <param name="actor"></param>
		/// <returns></returns>
		private Task UpdateModuleMaterialsInternalAsync(Module module, List<UpdateModuleMaterialDto>? materials, string actor)
		{
			if (materials is null || materials.Count == 0)
			{
				// Mark all existing materials as inactive (soft delete)
				foreach (var mat in module.ModuleMaterials.Where(m => m.IsActive))
				{
					mat.IsActive = false;
				}
				return Task.CompletedTask;
			}
			var now = DateTime.UtcNow;
			var existingMaterials = module.ModuleMaterials.ToDictionary(m => m.MaterialId, m => m);
			var payloadMaterialIds = materials.Where(m => m.MaterialId.HasValue).Select(m => m.MaterialId!.Value).ToHashSet();
			// 1. Mark materials not in payload as inactive (soft delete)
			foreach (var existing in existingMaterials.Values.Where(m => m.IsActive && !payloadMaterialIds.Contains(m.MaterialId)))
			{
				existing.IsActive = false;
			}
			// 2. Update existing materials or create new ones
			foreach (var matDto in materials)
			{
				if (matDto.MaterialId.HasValue && existingMaterials.TryGetValue(matDto.MaterialId.Value, out var existing))
				{
					// Update existing material
					existing.Title = matDto.Title;
					existing.Description = matDto.Description;
					existing.FileUrl = matDto.FileUrl;
					existing.IsActive = matDto.IsActive;
				}
				else
				{
					// Create new material - let EF generate the ID
					var newMaterial = new ModuleMaterial
					{
						ModuleId = module.ModuleId,
						Title = matDto.Title,
						Description = matDto.Description,
						FileUrl = matDto.FileUrl,
						IsActive = matDto.IsActive,
						CreatedAt = now,
						UpdatedAt = now,
						CreatedBy = actor,
						UpdatedBy = actor
					};
					module.ModuleMaterials.Add(newMaterial);
				}
			}
			return Task.CompletedTask;
		}

		/// <summary>
		/// Update Lessons based on payload
		/// </summary>
		private Task UpdateLessonsAsync(Module module, List<UpdateLessonDto> lessons, string currentUser)
		{
			if (lessons is null || lessons.Count == 0)
			{
				// Mark all existing lessons as inactive (soft delete)
				foreach (var lesson in module.Lessons.Where(l => l.IsActive))
				{
					lesson.IsActive = false;
				}
				return Task.CompletedTask;
			}

			var now = DateTime.UtcNow;
			var existingLessons = module.Lessons.ToDictionary(l => l.LessonId, l => l);
			var payloadLessonIds = lessons.Where(l => l.LessonId.HasValue).Select(l => l.LessonId!.Value).ToHashSet();

			// 1. Mark lessons not in payload as inactive (soft delete)
			foreach (var existing in existingLessons.Values.Where(l => l.IsActive && !payloadLessonIds.Contains(l.LessonId)))
			{
				existing.IsActive = false;
			}

			// 2. Update existing lessons or create new ones
			foreach (var lessonDto in lessons)
			{
				if (lessonDto.LessonId.HasValue && existingLessons.TryGetValue(lessonDto.LessonId.Value, out var existing))
				{
					// Update existing lesson
					existing.Title = lessonDto.Title;
					existing.VideoUrl = lessonDto.VideoUrl;
					existing.VideoDurationSec = lessonDto.VideoDurationSec;
					existing.PositionIndex = lessonDto.PositionIndex;
					existing.IsActive = lessonDto.IsActive;
				}
				else
				{
					// Create new lesson - let EF generate the ID
					var newLesson = new Lesson
					{
						ModuleId = module.ModuleId,
						Title = lessonDto.Title,
						VideoUrl = lessonDto.VideoUrl,
						VideoDurationSec = lessonDto.VideoDurationSec,
						PositionIndex = lessonDto.PositionIndex,
						IsActive = lessonDto.IsActive,
						CreatedAt = now,
						UpdatedAt = now,
						CreatedBy = currentUser,
						UpdatedBy = currentUser
					};
					module.Lessons.Add(newLesson);
				}
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Validate Module PositionIndex and Lesson PositionIndex constraints
		/// </summary>
		private static void ValidateModulePositionIndexes(UpdateModuleDto dto)
		{
			// Module Objectives (chỉ active)
			if (dto.Objectives is { Count: > 0 })
			{
				var activeIdx = dto.Objectives
					.Where(o => o.IsActive)
					.Select(o => o.PositionIndex)
					.ToList();

				if (activeIdx.Count != activeIdx.Distinct().Count())
					throw new ValidationException("Module Objective PositionIndex must be unique among active objectives.");

				if (activeIdx.Any(i => i <= 0))
					throw new ValidationException("Module Objective PositionIndex must be > 0 for active objectives.");
			}

			// Lessons (chỉ active)
			if (dto.Lessons is { Count: > 0 })
			{
				var activeIdx = dto.Lessons
					.Where(l => l.IsActive)
					.Select(l => l.PositionIndex)
					.ToList();

				if (activeIdx.Count != activeIdx.Distinct().Count())
					throw new ValidationException("Lesson PositionIndex must be unique among active lessons.");

				if (activeIdx.Any(i => i <= 0))
					throw new ValidationException("Lesson PositionIndex must be > 0 for active lessons.");
			}

			// Module Discussions (chỉ active)
			if (dto.Discussions is { Count: > 0 })
			{
				var activeIdx = dto.Discussions
					.Where(d => d.IsActive)
					.Select((d, index) => index + 1) // Giả sử PositionIndex là thứ tự trong danh sách
					.ToList();
				if (activeIdx.Count != activeIdx.Distinct().Count())
					throw new ValidationException("Module Discussion PositionIndex must be unique among active discussions.");
				if (activeIdx.Any(i => i <= 0))
					throw new ValidationException("Module Discussion PositionIndex must be > 0 for active discussions.");
			}

			// Module Materials (chỉ active)
			if (dto.Materials is { Count: > 0 })
			{
				var activeIdx = dto.Materials
					.Where(m => m.IsActive)
					.Select((m, index) => index + 1) // Giả sử PositionIndex là thứ tự trong danh sách
					.ToList();
				if (activeIdx.Count != activeIdx.Distinct().Count())
					throw new ValidationException("Module Material PositionIndex must be unique among active materials.");
				if (activeIdx.Any(i => i <= 0))
					throw new ValidationException("Module Material PositionIndex must be > 0 for active materials.");
			}

		}
	}
}
