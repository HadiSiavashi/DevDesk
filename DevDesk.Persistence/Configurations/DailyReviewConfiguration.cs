using DevDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevDesk.Persistence.Configurations;

public class DailyReviewConfiguration : IEntityTypeConfiguration<DailyReview>
{
    public void Configure(EntityTypeBuilder<DailyReview> builder)
    {
        builder.ToTable("DailyReviews");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Date)
            .HasColumnType("date");

        builder.Property(x => x.WhatWentWell)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.WhatDidNotGoWell)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.LessonsLearned)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.TomorrowPlan)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2");

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2");

        builder.Ignore(x => x.IsComplete);

        builder.HasIndex(x => x.Date)
            .IsUnique();
    }
}
