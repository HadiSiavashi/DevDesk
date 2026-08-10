using DevDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevDesk.Persistence.Configurations;

public class FocusSessionConfiguration : IEntityTypeConfiguration<FocusSession>
{
    public void Configure(EntityTypeBuilder<FocusSession> builder)
    {
        builder.ToTable("FocusSessions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.StartedAt)
            .HasColumnType("datetime2");

        builder.Property(x => x.EndedAt)
            .HasColumnType("datetime2");

        builder.Property(x => x.SessionType)
            .HasConversion<int>();

        builder.Property(x => x.Notes)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.PausedAt)
            .HasColumnType("datetime2");

        builder.Ignore(x => x.IsActive);

        builder.HasOne(x => x.Task)
            .WithMany(x => x.FocusSessions)
            .HasForeignKey(x => x.TaskId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Project)
            .WithMany(x => x.FocusSessions)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.PomodoroSession)
            .WithOne(x => x.FocusSession)
            .HasForeignKey<PomodoroSession>(x => x.FocusSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.TaskId);
        builder.HasIndex(x => x.ProjectId);
        builder.HasIndex(x => x.StartedAt);
    }
}
