using DevDesk.Application.Dtos;

namespace DevDesk.Application.Interfaces;

public interface IGoalService
{
    Task<GoalDto> CreateAsync(CreateGoalRequest request, CancellationToken ct = default);
    Task<GoalDto> UpdateAsync(Guid id, UpdateGoalRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<GoalDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<GoalDto>> GetAllAsync(bool includeCompleted = true, CancellationToken ct = default);
    Task<GoalDto> SetProgressAsync(Guid id, int progress, CancellationToken ct = default);
}
