using DevDesk.Application.Dtos;

namespace DevDesk.Application.Interfaces;

public interface IFocusService
{
    Task<FocusSessionDto> StartAsync(StartFocusRequest request, CancellationToken ct = default);
    Task<FocusSessionDto> PauseAsync(Guid sessionId, CancellationToken ct = default);
    Task<FocusSessionDto> ResumeAsync(Guid sessionId, CancellationToken ct = default);
    Task<FocusSessionDto> StopAsync(Guid sessionId, CancellationToken ct = default);
    Task<FocusSessionDto?> GetActiveAsync(CancellationToken ct = default);
    Task<FocusSessionDto?> RecoverActiveOnStartupAsync(CancellationToken ct = default);
    Task<FocusSessionDto> StartPomodoroAsync(StartPomodoroRequest request, CancellationToken ct = default);
    Task<FocusSessionDto> CompletePomodoroAsync(Guid sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<FocusSessionDto>> GetHistoryAsync(DateOnly? from = null, DateOnly? to = null, CancellationToken ct = default);
}
