using DevDesk.Application.Dtos;

namespace DevDesk.Application.Interfaces;

public interface IDailyPlanService
{
    Task<DailyPlanDto> GetOrCreateAsync(DateOnly date, CancellationToken ct = default);
    Task<DailyPlanDto> UpdateAsync(DateOnly date, UpdateDailyPlanRequest request, CancellationToken ct = default);
}
