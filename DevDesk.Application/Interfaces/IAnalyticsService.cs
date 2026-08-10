using DevDesk.Application.Dtos;

namespace DevDesk.Application.Interfaces;

public interface IAnalyticsService
{
    Task<AnalyticsDto> GetAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<ProductivityScoreDto> GetProductivityScoreAsync(DateOnly date, CancellationToken ct = default);
}
