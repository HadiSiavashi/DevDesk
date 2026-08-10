using DevDesk.Application.Abstractions;
using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.Application.Mapping;
using DevDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevDesk.Application.Services;

public sealed class HabitService(IDevDeskDbContext db, IClock clock) : IHabitService
{
    public async Task<HabitDto> CreateAsync(CreateHabitRequest request, CancellationToken ct = default)
    {
        var habit = new Habit
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description,
            Frequency = request.Frequency,
            IsActive = true,
            CreatedAt = clock.UtcNow
        };
        db.Habits.Add(habit);
        await db.SaveChangesAsync(ct);
        return await GetRequiredAsync(habit.Id, ct);
    }

    public async Task<HabitDto> UpdateAsync(Guid id, UpdateHabitRequest request, CancellationToken ct = default)
    {
        var habit = await db.Habits.FirstOrDefaultAsync(h => h.Id == id, ct)
            ?? throw new KeyNotFoundException($"Habit {id} was not found.");

        habit.Name = request.Name.Trim();
        habit.Description = request.Description;
        habit.Frequency = request.Frequency;
        habit.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
        return await GetRequiredAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var habit = await db.Habits.FirstOrDefaultAsync(h => h.Id == id, ct)
            ?? throw new KeyNotFoundException($"Habit {id} was not found.");
        db.Habits.Remove(habit);
        await db.SaveChangesAsync(ct);
    }

    public async Task<HabitDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var habit = await DetailQuery().FirstOrDefaultAsync(h => h.Id == id, ct);
        return habit?.ToDto(clock.Today);
    }

    public async Task<IReadOnlyList<HabitDto>> GetAllAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        var query = DetailQuery();
        if (activeOnly)
            query = query.Where(h => h.IsActive);

        var items = await query.OrderBy(h => h.Name).ToListAsync(ct);
        var today = clock.Today;
        return items.Select(h => h.ToDto(today)).ToList();
    }

    public async Task<HabitDto> ToggleCompletionAsync(Guid id, DateOnly date, CancellationToken ct = default)
    {
        _ = await db.Habits.FirstOrDefaultAsync(h => h.Id == id, ct)
            ?? throw new KeyNotFoundException($"Habit {id} was not found.");

        var record = await db.HabitRecords.FirstOrDefaultAsync(r => r.HabitId == id && r.Date == date, ct);
        if (record is null)
        {
            record = new HabitRecord
            {
                Id = Guid.NewGuid(),
                HabitId = id,
                Date = date,
                IsCompleted = true
            };
            db.HabitRecords.Add(record);
        }
        else
        {
            record.IsCompleted = !record.IsCompleted;
        }

        await db.SaveChangesAsync(ct);
        return await GetRequiredAsync(id, ct);
    }

    private IQueryable<Habit> DetailQuery() =>
        db.Habits.AsNoTracking().Include(h => h.Records);

    private async Task<HabitDto> GetRequiredAsync(Guid id, CancellationToken ct)
    {
        var habit = await DetailQuery().FirstOrDefaultAsync(h => h.Id == id, ct)
            ?? throw new KeyNotFoundException($"Habit {id} was not found.");
        return habit.ToDto(clock.Today);
    }
}
