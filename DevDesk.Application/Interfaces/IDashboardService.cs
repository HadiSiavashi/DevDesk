using DevDesk.Application.Dtos;

namespace DevDesk.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(CancellationToken ct = default);
}
