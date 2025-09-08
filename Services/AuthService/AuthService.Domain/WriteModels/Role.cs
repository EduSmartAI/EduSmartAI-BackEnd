namespace AuthService.Domain.WriteModels;

public partial class Role
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string NormalizedName { get; set; } = null!;
    
    public virtual ICollection<Account> Users { get; set; } = new List<Account>();
}
