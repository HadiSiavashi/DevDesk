using DevDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevDesk.Persistence.Configurations;

public class PomodoroSessionConfiguration : IEntityTypeConfiguration<PomodoroSession>
{
    public void Configure(EntityTypeBuilder<PomodoroSession> builder)
    {
        builder.ToTable("PomodoroSessions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.StartedAt)
            .HasColumnType("datetime2");

        builder.Property(x => x.EndedAt)
            .HasColumnType("datetime2");

        builder.HasIndex(x => x.FocusSessionId)
            .IsUnique();
    }
}
