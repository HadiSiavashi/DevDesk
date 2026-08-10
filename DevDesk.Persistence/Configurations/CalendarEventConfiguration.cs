using DevDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevDesk.Persistence.Configurations;

public class CalendarEventConfiguration : IEntityTypeConfiguration<CalendarEvent>
{
    public void Configure(EntityTypeBuilder<CalendarEvent> builder)
    {
        builder.ToTable("CalendarEvents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Description)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.StartAt)
            .HasColumnType("datetime2");

        builder.Property(x => x.EndAt)
            .HasColumnType("datetime2");

        builder.Property(x => x.EventType)
            .HasConversion<int>();

        builder.HasOne(x => x.Project)
            .WithMany(x => x.CalendarEvents)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Task)
            .WithMany(x => x.CalendarEvents)
            .HasForeignKey(x => x.TaskId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.StartAt);
        builder.HasIndex(x => x.EndAt);
    }
}
