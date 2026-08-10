using DevDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevDesk.Persistence.Configurations;

public class HabitRecordConfiguration : IEntityTypeConfiguration<HabitRecord>
{
    public void Configure(EntityTypeBuilder<HabitRecord> builder)
    {
        builder.ToTable("HabitRecords");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Date)
            .HasColumnType("date");

        builder.HasOne(x => x.Habit)
            .WithMany(x => x.Records)
            .HasForeignKey(x => x.HabitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.HabitId, x.Date })
            .IsUnique();

        builder.HasIndex(x => x.HabitId);
        builder.HasIndex(x => x.Date);
    }
}
