namespace Course.Application.DTOs.CoursesDTO;

public record CheckEnrollmentDto(
    Guid CourseId,
    bool IsEnrolled,
    DateTime? EnrolledAt,
    DateTime? ExpiresAt,
    bool IsActive
);
