using DevDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevDesk.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.Color)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Icon)
            .HasMaxLength(100);

        builder.Property(x => x.RepositoryUrl)
            .HasMaxLength(2000);

        builder.Property(x => x.LocalPath)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2");

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2");
    }
}
