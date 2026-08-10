using DevDesk.Application.Abstractions;
using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.Application.Mapping;
using DevDesk.Domain.Entities;
using DevDesk.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DevDesk.Application.Services;

public sealed class DailyPlanService(IDevDeskDbContext db, IClock clock) : IDailyPlanService
{
    public async Task<DailyPlanDto> GetOrCreateAsync(DateOnly date, CancellationToken ct = default)
    {
        var plan = await db.DailyPlans.FirstOrDefaultAsync(p => p.Date == date, ct);
        if (plan is null)
        {
            var now = clock.UtcNow;
            plan = new DailyPlan
            {
                Id = Guid.NewGuid(),
                Date = date,
                AvailableWorkMinutes = 480,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.DailyPlans.Add(plan);
            await db.SaveChangesAsync(ct);
        }

        var workload = await EstimateWorkloadAsync(date, ct);
        return plan.ToDto(workload);
    }

    public async Task<DailyPlanDto> UpdateAsync(DateOnly date, UpdateDailyPlanRequest request, CancellationToken ct = default)
    {
        var plan = await db.DailyPlans.FirstOrDefaultAsync(p => p.Date == date, ct);
        var now = clock.UtcNow;

        if (plan is null)
        {
            plan = new DailyPlan
            {
                Id = Guid.NewGuid(),
                Date = date,
                CreatedAt = now
            };
            db.DailyPlans.Add(plan);
        }

        // Exactly 3 goal slots (nullable allowed individually).
        plan.TopGoal1 = NormalizeGoal(request.TopGoal1);
        plan.TopGoal2 = NormalizeGoal(request.TopGoal2);
        plan.TopGoal3 = NormalizeGoal(request.TopGoal3);
        plan.Notes = request.Notes;
        plan.AvailableWorkMinutes = Math.Clamp(request.AvailableWorkMinutes, 0, 24 * 60);
        plan.UpdatedAt = now;

        await db.SaveChangesAsync(ct);
        var workload = await EstimateWorkloadAsync(date, ct);
        return plan.ToDto(workload);
    }

    private async Task<int> EstimateWorkloadAsync(DateOnly date, CancellationToken ct)
    {
        var tasks = await db.Tasks.AsNoTracking()
            .Where(t => t.DueDate.HasValue && t.Status != WorkTaskStatus.Done && t.Status != WorkTaskStatus.Cancelled)
            .ToListAsync(ct);

        return tasks
            .Where(t => DateOnly.FromDateTime(t.DueDate!.Value) == date)
            .Sum(t => t.EstimatedMinutes ?? 30);
    }

    private static string? NormalizeGoal(string? goal) =>
        string.IsNullOrWhiteSpace(goal) ? null : goal.Trim();
}
