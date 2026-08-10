using DevDesk.Application.Abstractions;
using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.Application.Mapping;
using DevDesk.Domain.Entities;
using DevDesk.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DevDesk.Application.Services;

public sealed class DailyReviewService(IDevDeskDbContext db, IClock clock) : IDailyReviewService
{
    public async Task<DailyReviewDto> GetOrCreateAsync(DateOnly date, CancellationToken ct = default)
    {
        var review = await db.DailyReviews.FirstOrDefaultAsync(r => r.Date == date, ct);
        if (review is null)
        {
            var now = clock.UtcNow;
            review = new DailyReview
            {
                Id = Guid.NewGuid(),
                Date = date,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.DailyReviews.Add(review);
            await db.SaveChangesAsync(ct);
        }

        await AutoFillAsync(review, date, ct);
        await db.SaveChangesAsync(ct);
        return review.ToDto();
    }

    public async Task<DailyReviewDto> UpdateAsync(DateOnly date, UpdateDailyReviewRequest request, CancellationToken ct = default)
    {
        var review = await db.DailyReviews.FirstOrDefaultAsync(r => r.Date == date, ct);
        var now = clock.UtcNow;

        if (review is null)
        {
            review = new DailyReview
            {
                Id = Guid.NewGuid(),
                Date = date,
                CreatedAt = now
            };
            db.DailyReviews.Add(review);
        }

        review.WhatWentWell = request.WhatWentWell;
        review.WhatDidNotGoWell = request.WhatDidNotGoWell;
        review.LessonsLearned = request.LessonsLearned;
        review.TomorrowPlan = request.TomorrowPlan;
        review.UpdatedAt = now;

        await AutoFillAsync(review, date, ct);
        await db.SaveChangesAsync(ct);
        return review.ToDto();
    }

    private async Task AutoFillAsync(DailyReview review, DateOnly date, CancellationToken ct)
    {
        var from = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var to = date.ToDateTime(new TimeOnly(23, 59, 59), DateTimeKind.Utc);

        var focusMinutes = await db.FocusSessions.AsNoTracking()
            .Where(s => s.EndedAt != null && s.StartedAt >= from && s.StartedAt <= to)
            .SumAsync(s => s.DurationMinutes, ct);

        var completed = await db.Tasks.AsNoTracking()
            .Where(t => t.Status == WorkTaskStatus.Done && t.CompletedAt != null && t.CompletedAt >= from && t.CompletedAt <= to)
            .CountAsync(ct);

        review.FocusMinutes = focusMinutes;
        review.CompletedTaskCount = completed;
        review.UpdatedAt = clock.UtcNow;
    }
}
