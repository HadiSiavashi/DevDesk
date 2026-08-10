using DevDesk.Application.Dtos;

namespace DevDesk.Application.Interfaces;

public interface IHabitService
{
    Task<HabitDto> CreateAsync(CreateHabitRequest request, CancellationToken ct = default);
    Task<HabitDto> UpdateAsync(Guid id, UpdateHabitRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<HabitDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<HabitDto>> GetAllAsync(bool activeOnly = true, CancellationToken ct = default);
    Task<HabitDto> ToggleCompletionAsync(Guid id, DateOnly date, CancellationToken ct = default);
}
