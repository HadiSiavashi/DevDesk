using DevDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevDesk.Persistence.Configurations;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("Attachments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.FilePath)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.ContentType)
            .HasMaxLength(200);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2");

        builder.HasOne(x => x.Task)
            .WithMany(x => x.Attachments)
            .HasForeignKey(x => x.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Note)
            .WithMany(x => x.Attachments)
            .HasForeignKey(x => x.NoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.TaskId);
        builder.HasIndex(x => x.NoteId);
    }
}
