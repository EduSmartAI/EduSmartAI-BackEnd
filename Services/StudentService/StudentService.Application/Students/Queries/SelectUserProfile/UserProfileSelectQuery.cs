using BuildingBlocks.CQRS;

namespace StudentService.Application.Students.Queries.SelectUserProfile;

public record UserProfileSelectQuery() : IQuery<StudentProfileSelectResponse>;
