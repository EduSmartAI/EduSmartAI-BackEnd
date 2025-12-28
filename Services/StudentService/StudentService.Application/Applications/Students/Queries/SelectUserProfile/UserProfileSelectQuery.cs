using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.Students.Queries.SelectUserProfile;

public record UserProfileSelectQuery() : IQuery<StudentProfileSelectResponse>;
