using DevDesk.Application.Abstractions;
using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.Application.Mapping;
using DevDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevDesk.Application.Services;

public sealed class GoalService(IDevDeskDbContext db, IClock clock) : IGoalService
{
    public async Task<GoalDto> CreateAsync(CreateGoalRequest request, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var goal = new Goal
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description,
            Category = request.Category,
            TargetDate = request.TargetDate,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Goals.Add(goal);
        await db.SaveChangesAsync(ct);
        return await GetRequiredAsync(goal.Id, ct);
    }

    public async Task<GoalDto> UpdateAsync(Guid id, UpdateGoalRequest request, CancellationToken ct = default)
    {
        var goal = await db.Goals.FirstOrDefaultAsync(g => g.Id == id, ct)
            ?? throw new KeyNotFoundException($"Goal {id} was not found.");

        goal.Title = request.Title.Trim();
        goal.Description = request.Description;
        goal.Category = request.Category;
        goal.TargetDate = request.TargetDate;
        goal.SetProgress(request.Progress, clock.UtcNow);

        await db.SaveChangesAsync(ct);
        return await GetRequiredAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var goal = await db.Goals.FirstOrDefaultAsync(g => g.Id == id, ct)
            ?? throw new KeyNotFoundException($"Goal {id} was not found.");
        db.Goals.Remove(goal);
        await db.SaveChangesAsync(ct);
    }

    public async Task<GoalDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var goal = await DetailQuery().FirstOrDefaultAsync(g => g.Id == id, ct);
        return goal?.ToDto();
    }

    public async Task<IReadOnlyList<GoalDto>> GetAllAsync(bool includeCompleted = true, CancellationToken ct = default)
    {
        var query = DetailQuery();
        if (!includeCompleted)
            query = query.Where(g => !g.IsCompleted);

        var items = await query
            .OrderBy(g => g.IsCompleted)
            .ThenBy(g => g.TargetDate)
            .ThenByDescending(g => g.UpdatedAt)
            .ToListAsync(ct);
        return items.Select(g => g.ToDto()).ToList();
    }

    public async Task<GoalDto> SetProgressAsync(Guid id, int progress, CancellationToken ct = default)
    {
        var goal = await db.Goals.FirstOrDefaultAsync(g => g.Id == id, ct)
            ?? throw new KeyNotFoundException($"Goal {id} was not found.");
        goal.SetProgress(progress, clock.UtcNow);
        await db.SaveChangesAsync(ct);
        return await GetRequiredAsync(id, ct);
    }

    private IQueryable<Goal> DetailQuery() =>
        db.Goals.AsNoTracking().Include(g => g.Milestones);

    private async Task<GoalDto> GetRequiredAsync(Guid id, CancellationToken ct)
    {
        var goal = await DetailQuery().FirstOrDefaultAsync(g => g.Id == id, ct)
            ?? throw new KeyNotFoundException($"Goal {id} was not found.");
        return goal.ToDto();
    }
}
