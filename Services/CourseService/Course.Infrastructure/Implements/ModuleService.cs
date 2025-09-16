using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using Course.Application.Courses.Commands.UpdateModule;
using Course.Application.DTOs.LessonsDTO;
using Course.Application.DTOs.ModulesDTO;
using Course.Application.Interfaces;
using Course.Domain.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Course.Infrastructure.Implements
{
	public class ModuleService(
		IModuleRepository _moduleRepository,
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
					x => x.Lessons)
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

		private async Task UpdateModuleObjectivesAsync(Module module, List<UpdateModuleObjectiveDto>? objectives, string currentUser)
		{
			if (objectives is null || objectives.Count == 0)
			{
				// Mark all existing objectives as inactive (soft delete)
				foreach (var obj in module.ModuleObjectives.Where(o => o.IsActive))
				{
					obj.IsActive = false;
					obj.UpdatedAt = DateTime.UtcNow;
					obj.UpdatedBy = currentUser;
				}
				return;
			}

			var now = DateTime.UtcNow;
			var existingObjectives = module.ModuleObjectives.ToDictionary(o => o.ObjectiveId, o => o);
			var payloadObjectiveIds = objectives.Where(o => o.ObjectiveId.HasValue).Select(o => o.ObjectiveId!.Value).ToHashSet();

			// 1. Mark objectives not in payload as inactive (soft delete)
			foreach (var existing in existingObjectives.Values.Where(o => o.IsActive && !payloadObjectiveIds.Contains(o.ObjectiveId)))
			{
				existing.IsActive = false;
				existing.UpdatedAt = now;
				existing.UpdatedBy = currentUser;
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
					existing.UpdatedAt = now;
					existing.UpdatedBy = currentUser;
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
		}

		/// <summary>
		/// Update Lessons based on payload
		/// </summary>
		private async Task UpdateLessonsAsync(Module module, List<UpdateLessonDto> lessons, string currentUser)
		{
			if (lessons is null || lessons.Count == 0)
			{
				// Mark all existing lessons as inactive (soft delete)
				foreach (var lesson in module.Lessons.Where(l => l.IsActive))
				{
					lesson.IsActive = false;
					lesson.UpdatedAt = DateTime.UtcNow;
					lesson.UpdatedBy = currentUser;
				}
				return;
			}

			var now = DateTime.UtcNow;
			var existingLessons = module.Lessons.ToDictionary(l => l.LessonId, l => l);
			var payloadLessonIds = lessons.Where(l => l.LessonId.HasValue).Select(l => l.LessonId!.Value).ToHashSet();

			// 1. Mark lessons not in payload as inactive (soft delete)
			foreach (var existing in existingLessons.Values.Where(l => l.IsActive && !payloadLessonIds.Contains(l.LessonId)))
			{
				existing.IsActive = false;
				existing.UpdatedAt = now;
				existing.UpdatedBy = currentUser;
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
					existing.UpdatedAt = now;
					existing.UpdatedBy = currentUser;
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
		}
	}
}
