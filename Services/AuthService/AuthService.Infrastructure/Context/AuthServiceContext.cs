using AuthService.Domain.WriteModels;
using BaseService.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Context;

public class AuthServiceContext(DbContextOptions<AuthServiceContext> options) : AppDbContext(options)
{
    public virtual DbSet<Account> Accounts { get; set; }
    
    public virtual DbSet<Role> Roles { get; set; }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.UseOpenIddict();

        builder.Entity<Account>(entity =>
        {
            entity.HasKey(x => x.AccountId);
            entity.Property(x => x.Email);
            entity.Property(x => x.PasswordHash).HasMaxLength(512);
            entity.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            entity.Property(x => x.UpdatedBy).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Key).HasMaxLength(256);
            entity.Property(x => x.LockoutEnd);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.AccessFailedCount).HasDefaultValue(0);
            entity.Property(x => x.EmailConfirmed).HasDefaultValue(false);

            entity.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        builder.Entity<Role>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.Property(x => x.NormalizedName).HasMaxLength(256).IsRequired();
        });
    }
}