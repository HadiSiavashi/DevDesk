using DevDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevDesk.Persistence.Configurations;

public class DailyPlanConfiguration : IEntityTypeConfiguration<DailyPlan>
{
    public void Configure(EntityTypeBuilder<DailyPlan> builder)
    {
        builder.ToTable("DailyPlans");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Date)
            .HasColumnType("date");

        builder.Property(x => x.TopGoal1)
            .HasMaxLength(500);

        builder.Property(x => x.TopGoal2)
            .HasMaxLength(500);

        builder.Property(x => x.TopGoal3)
            .HasMaxLength(500);

        builder.Property(x => x.Notes)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2");

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2");

        builder.Ignore(x => x.HasAllGoals);

        builder.HasIndex(x => x.Date)
            .IsUnique();
    }
}
