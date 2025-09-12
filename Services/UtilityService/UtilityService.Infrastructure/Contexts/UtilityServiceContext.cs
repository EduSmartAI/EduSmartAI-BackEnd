using BaseService.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using UtilityService.Domain.Models;

namespace UtilityService.Infrastructure.Contexts;

public partial class UtilityServiceContext : AppDbContext
{
    public UtilityServiceContext(DbContextOptions<UtilityServiceContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CloudinaryConfig> CloudinaryConfigs { get; set; }

    public virtual DbSet<Emailtemplate> Emailtemplates { get; set; }

    public virtual DbSet<Systemconfig> Systemconfigs { get; set; }

    public virtual DbSet<VwVerifyaccount> VwVerifyaccounts { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CloudinaryConfig>(entity =>
        {
            entity.HasKey(e => e.CloudApiKey).HasName("cloudinary_config_key");

            entity.ToTable("cloudinary_config");

            entity.Property(e => e.CloudApiKey).HasColumnName("cloud_api_key");
            entity.Property(e => e.CloudApiName).HasColumnName("cloud_api_name");
            entity.Property(e => e.CloudApiSecret).HasColumnName("cloud_api_secret");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("timezone('utc'::text, now())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("timezone('utc'::text, now())")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .HasColumnName("updated_by");
        });

        modelBuilder.Entity<Emailtemplate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("emailtemplate_pkey");

            entity.ToTable("emailtemplate");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Body).HasColumnName("body");
            entity.Property(e => e.CreateBy)
                .HasMaxLength(255)
                .HasColumnName("create_by");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("timezone('utc'::text, now())")
                .HasColumnName("created_at");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.ScreenName)
                .HasMaxLength(255)
                .HasColumnName("screen_name");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.UpdateBy)
                .HasMaxLength(255)
                .HasColumnName("update_by");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("timezone('utc'::text, now())")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Systemconfig>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("systemconfig_pkey");

            entity.ToTable("systemconfig");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("timezone('utc'::text, now())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("timezone('utc'::text, now())")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .HasColumnName("updated_by");
            entity.Property(e => e.Value).HasColumnName("value");
        });

        modelBuilder.Entity<VwVerifyaccount>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_verifyaccount");

            entity.Property(e => e.Body).HasColumnName("body");
            entity.Property(e => e.ScreenName)
                .HasMaxLength(255)
                .HasColumnName("screen_name");
            entity.Property(e => e.Title).HasColumnName("title");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
