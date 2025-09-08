using AuthService.Domain.WriteModels;

namespace AuthService.Domain.ReadModels;

public class RoleCollection
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string NormalizedName { get; set; } = null!;

    public static RoleCollection FromWriteModel(Role role)
    {
        return new RoleCollection
        {
            Id = role.Id,
            Name = role.Name,
            NormalizedName = role.NormalizedName
        };
    }
}