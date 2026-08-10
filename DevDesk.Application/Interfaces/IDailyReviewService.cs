using DevDesk.Application.Dtos;

namespace DevDesk.Application.Interfaces;

public interface IDailyReviewService
{
    Task<DailyReviewDto> GetOrCreateAsync(DateOnly date, CancellationToken ct = default);
    Task<DailyReviewDto> UpdateAsync(DateOnly date, UpdateDailyReviewRequest request, CancellationToken ct = default);
}
